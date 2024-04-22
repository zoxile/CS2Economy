using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text;
using MySqlConnector;

namespace CS2Economy;

public partial class CS2Economy
{
    public static string prefix { get; set; }

    [ConsoleCommand("css_balance")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBalanceCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int balance = GetPlayerBalance(caller);
        caller.PrintToChat($"{prefix} Your balance is {ChatColors.Green}{balance}");
    }

    [ConsoleCommand("css_flex")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnFlexCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int balance = GetPlayerBalance(caller);
        Server.PrintToChatAll($"{prefix} Your balance is {ChatColors.Green}{balance}");
    }

    [ConsoleCommand("css_gift")]
    [CommandHelper(minArgs: 2, usage: "<player> <amount>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnGiftCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int balance = GetPlayerBalance(caller);

        TargetResult? target = GetTarget(command);
        List<CCSPlayerController> playersToTarget = target!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot give minus credits!");
            return;
        }

        if (amount > balance)
        {
            caller.PrintToChat($"{prefix} You do not have sufficient credits.");
            return;
        }
        RemovePlayerCredits(caller, amount);

        playersToTarget.ForEach(player =>
        {
            if (!connectedPlayers.Contains(player))
            {
                caller.PrintToChat("Invalid player");
                return;
            }
            GivePlayerCredits(player, amount);
            Server.PrintToChatAll($"{prefix} {ChatColors.Red}{caller.PlayerName}{ChatColors.Lime} has gifted {ChatColors.Red}{player.PlayerName} {ChatColors.Purple}{amount}{ChatColors.Lime} Credits!");
        });
    }

    [ConsoleCommand("css_pay")]
    [CommandHelper(minArgs: 2, usage: "<player> <amount>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int balance = GetPlayerBalance(caller);

        TargetResult? target = GetTarget(command);
        List<CCSPlayerController> playersToTarget = target!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot give minus credits!");
            return;
        }

        if (amount > balance)
        {
            caller.PrintToChat($"{prefix} You do not have sufficient credits.");
            return;
        }
        RemovePlayerCredits(caller, amount);

        playersToTarget.ForEach(player =>
        {
            if (!connectedPlayers.Contains(player))
            {
                caller.PrintToChat("Invalid player");
                return;
            }
            GivePlayerCredits(player, amount);
            caller.PrintToChat($"{prefix} {ChatColors.Red}You{ChatColors.Lime} payed {ChatColors.Red}{player.PlayerName} {ChatColors.Purple}{amount}{ChatColors.Lime} Credits!");
            player.PrintToChat($"{prefix} {ChatColors.Red}{caller.PlayerName}{ChatColors.Lime} payed {ChatColors.Red}you {ChatColors.Purple}{amount}{ChatColors.Lime} Credits!");
        });
    }

    [ConsoleCommand("css_givecredits")]
    [CommandHelper(minArgs: 2, usage: "<player> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/cheats")]
    public void OnGiveCreditsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        TargetResult? target = GetTarget(command);
        List<CCSPlayerController> playersToTarget = target!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot give minus credits!");
            return;
        }

        playersToTarget.ForEach(player =>
        {
            if (!connectedPlayers.Contains(player))
            {
                caller.PrintToChat("Invalid player");
                return;
            }
            GivePlayerCredits(player, amount);
            Server.PrintToChatAll($"{prefix} {ChatColors.Red}{caller.PlayerName}{ChatColors.Lime} has given {ChatColors.Red}{player.PlayerName} {ChatColors.Purple}{amount}{ChatColors.Lime} Credits!");
        });
    }

    [ConsoleCommand("css_secretcredit")]
    [CommandHelper(minArgs: 2, usage: "<player> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/cheats")]
    public void OnGiveCreditsSecretCommand(CCSPlayerController? caller, CommandInfo command)
    {
        TargetResult? target = GetTarget(command);
        List<CCSPlayerController> playersToTarget = target!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot give minus credits!");
            return;
        }

        playersToTarget.ForEach(player =>
        {
            if (!connectedPlayers.Contains(player))
            {
                caller.PrintToChat("Invalid player");
                return;
            }
            GivePlayerCredits(player, amount);
            caller.PrintToChat($"{prefix} {ChatColors.Red}You{ChatColors.Lime} gave {ChatColors.Red}{player.PlayerName} {ChatColors.Purple}{amount}{ChatColors.Lime} Credits!");
            player.PrintToChat($"{prefix} {ChatColors.Red}{caller.PlayerName}{ChatColors.Lime} gave {ChatColors.Red}you {ChatColors.Purple}{amount}{ChatColors.Lime} Credits!");
        });
    }

    [ConsoleCommand("css_removecredits")]
    [CommandHelper(minArgs: 2, usage: "<player> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/cheats")]
    public void OnRemoveCreditsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        TargetResult? target = GetTarget(command);
        List<CCSPlayerController> playersToTarget = target!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot remove minus credits!");
            return;
        }

        playersToTarget.ForEach(player =>
        {
            if (!connectedPlayers.Contains(player))
            {
                caller.PrintToChat("Invalid player");
                return;
            }
            RemovePlayerCredits(player, amount);
            caller.PrintToChat($"{prefix} {ChatColors.Red}You removed {ChatColors.Purple}{amount} {ChatColors.Red}Credits from {player.PlayerName}");
        });
    }

    [ConsoleCommand("css_removecreditsoffline")]
    [CommandHelper(minArgs: 2, usage: "<steamid> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/cheats")]
    public void OnRemoveCreditsOfflineCommand(CCSPlayerController? caller, CommandInfo command)
    {

        string steamId = command.GetArg(1);
        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot remove minus credits!");
            return;
        }

        Task.Run(async () =>
            {
                RemoveCredits(steamId, amount);
            });
        RemovePlayerCreditsOffline(steamId, amount);

        caller.PrintToChat("CAUTION ONLY USE ON OFFLINE PLAYERS!");
    }

    [ConsoleCommand("css_givecreditsoffline")]
    [CommandHelper(minArgs: 2, usage: "<steamid> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/cheats")]
    public void OnGiveCreditsOfflineCommand(CCSPlayerController? caller, CommandInfo command)
    {

        string steamId = command.GetArg(1);
        int amount = 5;
        int.TryParse(command.GetArg(2), out amount);
        if (amount < 0)
        {
            caller.PrintToChat($"{prefix} You cannot give minus credits!");
            return;
        }

        Task.Run(async () =>
            {
                AddCredits(steamId, amount);
            });

        GivePlayerCreditsOffline(steamId, amount);

        caller.PrintToChat("CAUTION ONLY USE ON OFFLINE PLAYERS!");
    }

    [ConsoleCommand("css_roulette")]
    [CommandHelper(minArgs: 1, usage: "<bet>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRouletteClientCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (int.Parse(command.GetArg(1)) > 1000)
        {
            caller.PrintToChat($"{prefix} {ChatColors.Lime}Bet limit is between 100-1000 credits!");
            return;
        }
        int deposit = int.Parse(command.GetArg(1));
        int bank = GetPlayerBalance(caller);
        if (deposit > bank)
        {
            caller.PrintToChat($" {prefix} {ChatColors.Red}Insufficient credits!");
            return;
        }
        OnRouletteCommand(caller, command);
    }

    [ConsoleCommand("css_roulettetoggle")]
    [RequiresPermissions("@css/rcon")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRouletteServerConfigCommand(CCSPlayerController? caller, CommandInfo command)
    {
        OnRouletteDisableCommand(caller, command);
    }

    [ConsoleCommand("css_removemodels", "Remove Models")]
    [ConsoleCommand("css_mka", "Remove Models")]
    [RequiresPermissions("@css/changemap")]
    public void OnModelRemoveCommand(CCSPlayerController? caller, CommandInfo info)
    {
        if (caller.IsBot || caller == null || !caller.IsValid || caller.IsHLTV) return;
        Server.PrintToChatAll(($"{prefix} {caller.PlayerName} {ChatColors.Lime}has {ChatColors.Red}disabled{ChatColors.Red} all models!"));
        RemoveAllWornModels();
    }

}