using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;
using Dapper;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CS2Economy;

public partial class CS2Economy
{
    public void RewardPlayers()
    {
        int amount = Config.CreditReward;
        foreach (CCSPlayerController player in connectedPlayers)
        {
            PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
            if (Player != null)
            {
                if (AdminManager.PlayerHasPermissions(player, new String[] { "@css/vip" }) && Config.PlusRewardVIP)
                {
                    Player.Balance += (amount * 2);
                    player.PrintToChat($"{prefix} You have gotten {amount * 2} Credits as a reward!");
                }
                else
                {
                    Player.Balance += amount;
                    player.PrintToChat($"{prefix} You have gotten {amount} Credits as a reward!");
                }
            }
        }
    }
}