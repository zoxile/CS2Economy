using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Admin;
using System.Drawing;
using System.Text;
using static CounterStrikeSharp.API.Utilities;

namespace CS2Economy
{
    public partial class CS2Economy
    {
        public static bool _isEnabled { get; set; } = true;
        private static bool dontAllowDeposit { get; set; } = false;
        private bool[] playerGreenSlots = new bool[64];
        private bool[] playerRedSlots = new bool[64];
        private bool[] playerBlackSlots = new bool[64];
        private bool[] canPlayerRoulette = new bool[64];
        private int[] deposit = new int[64];
        private Dictionary<string, int> _RouletteSelections = new();

        private void RouletteSelectionSystem(CCSPlayerController player, ChatMenuOption option)
        {
            if (!_RouletteSelections.ContainsKey(option.Text))
            {
                _RouletteSelections[option.Text] = 0;
            }

            _RouletteSelections[option.Text]++;

            if (playerGreenSlots[player.Slot] || playerRedSlots[player.Slot] || playerBlackSlots[player.Slot])
            {
                player.PrintToChat($"{prefix} {ChatColors.Red}You have already placed your bet!");
                return;
            }
            if (dontAllowDeposit)
            {
                return;
            }

            if (option.Text == $" {ChatColors.Red}Red")
            {
                playerRedSlots[player.Slot] = true;
                playerGreenSlots[player.Slot] = false;
                playerBlackSlots[player.Slot] = false;
                player.PrintToChat($"{prefix} {ChatColors.Lime}You placed {ChatColors.Gold}{deposit[player.Slot]} Credits on {ChatColors.Red}Red");
                RemovePlayerCredits(player, deposit[player.Slot]);
            }
            else if (option.Text == $" {ChatColors.Green}Green")
            {
                playerRedSlots[player.Slot] = false;
                playerGreenSlots[player.Slot] = true;
                playerBlackSlots[player.Slot] = false;
                player.PrintToChat($"{prefix} {ChatColors.Lime}You placed {ChatColors.Gold}{deposit[player.Slot]} Credits on {ChatColors.Red}Green");
                RemovePlayerCredits(player, deposit[player.Slot]);
            }
            else if (option.Text == $" {ChatColors.Default}Black")
            {
                playerRedSlots[player.Slot] = false;
                playerGreenSlots[player.Slot] = false;
                playerBlackSlots[player.Slot] = true;
                player.PrintToChat($"{prefix} {ChatColors.Lime}You placed {ChatColors.Gold}{deposit[player.Slot]} Credits into {ChatColors.Red}Black");
                RemovePlayerCredits(player, deposit[player.Slot]);
            }
        }

        public void OnRouletteCommand(CCSPlayerController? caller, CommandInfo command)
        {
            dontAllowDeposit = false;
            if (!canPlayerRoulette[caller.Slot])
            {
                if (caller == null || !caller.IsValid)
                {
                    return;
                }
                if (!_isEnabled)
                {
                    caller.PrintToChat($"{prefix} {ChatColors.Red}Roulette is disabled at the moment!");
                    return;
                }
                if (command.ArgCount < 1)
                {
                    caller.PrintToChat($"{prefix} {ChatColors.Red}Invalid bet!");
                    return;
                }

                deposit[caller.Slot] = int.Parse(command.GetArg(1));
                int bank = GetPlayerBalance(caller);
                if (deposit[caller.Slot] > bank)
                {
                    caller.PrintToChat($"{prefix} {ChatColors.Lime}Insufficient credits!");
                    return;
                }

                if (deposit[caller.Slot] > 1000 || deposit[caller.Slot] <= 99)
                {
                    caller.PrintToChat($"{prefix} {ChatColors.Lime}Bet limit is between 100-1000 credits!");
                    return;
                }

                if (playerGreenSlots[caller.Slot] || playerRedSlots[caller.Slot] || playerBlackSlots[caller.Slot])
                {
                    caller.PrintToChat($"{prefix} {ChatColors.Red}You have already bet on something!");
                    return;
                }

                var question = $" {ChatColors.Purple}{prefix} {ChatColors.Gold}Roulette";
                var RouletteMenu = new ChatMenu(question);
                RouletteMenu.AddMenuOption($" {ChatColors.Red}Red", RouletteSelectionSystem);
                RouletteMenu.AddMenuOption($" {ChatColors.Default}Black", RouletteSelectionSystem);
                RouletteMenu.AddMenuOption($" {ChatColors.Green}Green", RouletteSelectionSystem);
                ChatMenus.OpenMenu(caller, RouletteMenu);
                canPlayerRoulette[caller.Slot] = true;
            }
            else
            {
                caller.PrintToChat($"{prefix} {ChatColors.Red}You have already bet on something!");
            }
        }

        public void OnRouletteDisableCommand(CCSPlayerController? caller, CommandInfo command)
        {
            if (caller == null)
            {
                return;
            }
            else
            {
                if (_isEnabled)
                {
                    _isEnabled = false;
                    Server.PrintToChatAll($"{prefix} {ChatColors.Gold}{caller.PlayerName} has disabled roulette!");
                }
                else
                {
                    _isEnabled = true;
                    Server.PrintToChatAll($"{prefix} {ChatColors.Gold}{caller.PlayerName} has enabled roulette!");
                }
            }
        }

        public void StartRoulette()
        {
            Server.PrintToChatAll($" {prefix} {ChatColors.Purple}Roulette {ChatColors.Default}will start in {ChatColors.Lime}10 {ChatColors.Default}seconds!");
            AddTimer(7, () =>
            {
                CountDownAnnouncer();
                AddTimer(3, () =>
                {
                    dontAllowDeposit = true;
                    Array.Clear(canPlayerRoulette, 0, canPlayerRoulette.Length);
                    Server.PrintToChatAll($"{prefix} Roulette is spinning! {ChatColors.Green}Good Luck!");
                    int random = GenerateRandomNumber(1, 100);

                    AddTimer(3, () =>
                    {
                        if (random <= 48)
                        {
                            OnBlackWon();
                        }
                        else if (random >= 48 && random <= 97)
                        {
                            OnRedWon();
                        }
                        else if (random >= 97)
                        {
                            OnGreenWon();
                        }
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }
        private void CountDownAnnouncer()
        {
            for (int i = 3; i > 0; i--)
            {
                int countdownValue = i;
                AddTimer(3 - i, () =>
                {
                    Server.PrintToChatAll($" {prefix} {ChatColors.Purple}Roulette {ChatColors.Default}is spinning in {ChatColors.Lime}{countdownValue} {ChatColors.Default}seconds!");
                });
            }
        }
        public void OnBlackWon()
        {
            Server.PrintToChatAll($"{prefix} {ChatColors.Purple}Roulette: {ChatColors.Default}Black {ChatColors.Gold}won!");
            var players = Utilities.GetPlayers().Where(pawn => pawn is { IsValid: true, IsBot: false, IsHLTV: false });
            foreach (var player in players)
            {
                if (playerBlackSlots[player.Slot])
                {
                    int totalWinning = (int)(deposit[player.Slot] * 1.9);
                    GivePlayerCredits(player, totalWinning);
                    if (deposit[player.Slot] > 99)
                    {
                        player.PrintToChat($"{prefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.Green} got {totalWinning} credits!");
                    }
                    playerBlackSlots[player.Slot] = false;
                    playerRedSlots[player.Slot] = false;
                    playerGreenSlots[player.Slot] = false;
                }
                else if (playerGreenSlots[player.Slot] || playerRedSlots[player.Slot])
                {
                    player.PrintToChat($"{prefix} {ChatColors.Grey}Unfortunately you lost... Maybe next time?");
                    playerBlackSlots[player.Slot] = false;
                    playerRedSlots[player.Slot] = false;
                    playerGreenSlots[player.Slot] = false;
                }

            }
            Array.Clear(deposit, 0, deposit.Length);
            Array.Clear(playerBlackSlots, 0, playerBlackSlots.Length);
        }
        public void OnRedWon()
        {
            Server.PrintToChatAll($"{prefix} {ChatColors.Purple}Roulette: {ChatColors.Red}Red {ChatColors.Gold}won!");
            var players = Utilities.GetPlayers().Where(pawn => pawn is { IsValid: true, IsBot: false, IsHLTV: false });
            foreach (var player in players)
            {
                if (playerRedSlots[player.Slot])
                {
                    int totalWinning = (int)(deposit[player.Slot] * 1.9);
                    GivePlayerCredits(player, totalWinning);
                    if (deposit[player.Slot] > 99)
                    {
                        player.PrintToChat($"{prefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.Green} got {totalWinning} credits!");
                    }
                    playerBlackSlots[player.Slot] = false;
                    playerRedSlots[player.Slot] = false;
                    playerGreenSlots[player.Slot] = false;
                }
                else if (playerGreenSlots[player.Slot] || playerBlackSlots[player.Slot])
                {
                    player.PrintToChat($"{prefix} {ChatColors.Grey}Unfortunately you lost... Maybe next time?");
                    playerBlackSlots[player.Slot] = false;
                    playerRedSlots[player.Slot] = false;
                    playerGreenSlots[player.Slot] = false;
                }
            }
            Array.Clear(deposit, 0, deposit.Length);
            Array.Clear(playerRedSlots, 0, playerRedSlots.Length);

        }
        public void OnGreenWon()
        {
            Server.PrintToChatAll($"{prefix} {ChatColors.Purple}Roulette: {ChatColors.Green}Green {ChatColors.Gold}won!");
            var players = Utilities.GetPlayers().Where(pawn => pawn is { IsValid: true, IsBot: false, IsHLTV: false });
            foreach (var player in players)
            {
                if (playerGreenSlots[player.Slot])
                {
                    int totalWinning = (int)(deposit[player.Slot] * 13.9);
                    GivePlayerCredits(player, totalWinning);
                    if (deposit[player.Slot] > 99)
                    {
                        player.PrintToChat($"{prefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.Green} got {totalWinning} credits!");
                    }
                    playerBlackSlots[player.Slot] = false;
                    playerRedSlots[player.Slot] = false;
                    playerGreenSlots[player.Slot] = false;
                }
                else if (playerRedSlots[player.Slot] || playerBlackSlots[player.Slot])
                {
                    player.PrintToChat($"{prefix} {ChatColors.Grey}Unfortunately you lost... Maybe next time?");
                    playerBlackSlots[player.Slot] = false;
                    playerRedSlots[player.Slot] = false;
                    playerGreenSlots[player.Slot] = false;
                }
            }
            Array.Clear(deposit, 0, deposit.Length);
            Array.Clear(playerGreenSlots, 0, playerGreenSlots.Length);
        }

        private int GenerateRandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}
