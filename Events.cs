using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using static Dapper.SqlMapper;

namespace CS2Economy;

public partial class CS2Economy
{
    public static List<CCSPlayerController> connectedPlayers = new List<CCSPlayerController>();

    public void RegisterEvents()
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Pre);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Pre);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        foreach (CCSPlayerController player in connectedPlayers)
        {
            PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
            int balance = Player.Balance;
            string playername = player.PlayerName;
            string steamID = GetSteamID(player);
            if (balance < 0) return HookResult.Continue;

            Task.Run(async () =>
            {
                UpdateCredits(playername, steamID, balance);
                if (Player.WornModelCT > 0 || Player.WornModelT > 0)
                {
                    UpdateWornModel(steamID, Player.WornModelCT, Player.WornModelT);
                }
            });
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
        {
            return HookResult.Continue;
        }

        InitPlayer(player);

        return HookResult.Continue;
    }
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
        {
            return HookResult.Continue;
        }

        else
        {
            if (connectedPlayers.Contains(player))
            {
                PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
                int balance = Player.Balance;
                string playername = player.PlayerName;
                string steamID = GetSteamID(player);

                Task.Run(async () =>
                {
                    UpdateCredits(playername, steamID, balance);
                    if (Player.WornModelCT > 0 || Player.WornModelT > 0)
                    {
                        UpdateWornModel(steamID, Player.WornModelT, Player.WornModelCT);
                    }
                });
                connectedPlayers.Remove(player);
            }
            return HookResult.Continue;
        }
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController player = @event.Userid;

        try
        {
            if (!connectedPlayers.Contains(player))
            {
                Console.WriteLine("[CS2Economy] Player not found on spawn");
                return HookResult.Continue;
            }

            PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);

            if (Player != null)
            {
                if (player.TeamNum == 2 && Player.WornModelT != -1)
                {
                    Models targetModel = models.FirstOrDefault(m => m.Modelid == Player.WornModelT)!;
                    setPlayerModel(player.PlayerPawn.Value, targetModel.ModelPath);
                }

                else if (player.TeamNum == 3 && Player.WornModelCT != -1)
                {
                    Models targetModel = models.FirstOrDefault(m => m.Modelid == Player.WornModelCT)!;
                    setPlayerModel(player.PlayerPawn.Value, targetModel.ModelPath);
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine("ERROR! COULD NOT SET MODEL!");
        }

        return HookResult.Continue;
    }

    public async void InitPlayer(CCSPlayerController player)
    {
        if (player.IsBot || !player.IsValid)
        {
            return;
        }
        else
        {
            int? playerUserId = player.UserId;
            string playerSteam = GetSteamID(player);
            string playerName = player.PlayerName;

            var newPlayer = new PlayerCredentials(player, playerUserId, playerName, playerSteam);

            // Checking database here for existing data, otherwise create new.
            var existingPlayerData = await GetPlayerDataFromDB(playerSteam);

            if (existingPlayerData != null)
            {
                newPlayer.Balance = existingPlayerData.credits;
                newPlayer.WornModelCT = existingPlayerData.lastwornct;
                newPlayer.WornModelT = existingPlayerData.lastwornt;
            }

            playerList.Add(newPlayer);

            SetupPlayer(playerSteam, playerName);
            Task.Run(async () =>
            {
                await UpdateOwnedModelsFromDatabase(playerSteam);
                await UpdatePlayerWornModelsFromDatabase(playerSteam);
            }).Wait();

            connectedPlayers.Add(player);
        }
    }

}