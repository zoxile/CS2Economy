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
using CounterStrikeSharp.API.Modules.Menu;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using static Dapper.SqlMapper;

namespace CS2Economy;

public partial class CS2Economy
{

    [ConsoleCommand("css_shop", "Opens the store menu")]
    [ConsoleCommand("css_market", "Opens the store menu")]
    [ConsoleCommand("css_store", "Opens the store menu")]
    public void OnStoreCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var title = $"{prefix} - {ChatColors.Purple}| Store Menu |";
        var storeMenu = new ChatMenu(title);
        storeMenu.AddMenuOption($" {ChatColors.Blue}CT Models", ModelMenu);
        storeMenu.AddMenuOption($" {ChatColors.Gold}T Models", ModelMenu);

        ChatMenus.OpenMenu(caller, storeMenu);
    }

    private void ModelMenu(CCSPlayerController? caller, ChatMenuOption option)
    {
        PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == caller);
        string ct = $" {ChatColors.Blue}CT Models";
        string t = $" {ChatColors.Gold}T Models";

        if (option.Text == ct)
        {
            var titlect = $"{prefix} - {ChatColors.Purple}| Store CT Models |";
            var modelMenuCT = new ChatMenu(titlect);
            foreach (Models model in models.Where(model => model.AllowedTeam == "CT"))
            {
                if (Player.WornModelCT == model.Modelid)
                {
                    modelMenuCT.AddMenuOption($"{model.Modelname} - {ChatColors.Green}[USING]", SelectedModelMenu);
                }
                else if (Player.OwnedModels.Contains(model.Modelid))
                {
                    modelMenuCT.AddMenuOption($"{model.Modelname} - {ChatColors.Gold}[OWNED]", SelectedModelMenu);
                }
                else
                {
                    modelMenuCT.AddMenuOption($"{model.Modelname} - {ChatColors.Red}{model.Price} Credits", SelectedModelMenu);
                }
            }

            ChatMenus.OpenMenu(caller, modelMenuCT);
        }
        else if (option.Text == t)
        {
            var titlet = $"{prefix} - {ChatColors.Purple}| Store T Models |";
            var modelMenuT = new ChatMenu(titlet);
            foreach (Models model in models.Where(model => model.AllowedTeam == "T"))
            {
                if (Player.WornModelT == model.Modelid)
                {
                    modelMenuT.AddMenuOption($"{model.Modelname} - {ChatColors.Green}[USING]", SelectedModelMenu);
                }
                else if (Player.OwnedModels.Contains(model.Modelid))
                {
                    modelMenuT.AddMenuOption($"{model.Modelname} - {ChatColors.Gold}[OWNED]", SelectedModelMenu);
                }
                else
                {
                    modelMenuT.AddMenuOption($"{model.Modelname} - {ChatColors.Red}{model.Price} Credits", SelectedModelMenu);
                }
            }

            ChatMenus.OpenMenu(caller, modelMenuT);
        }
    }


    public void SelectedModelMenu(CCSPlayerController? caller, ChatMenuOption option)
    {
        var selectedModelName = option.Text.Split('-')[0].Trim();
        var selectedModel = models.FirstOrDefault(m => m.Modelname == selectedModelName);

        if (selectedModel == null)
        {
            caller?.PrintToChat($" {ChatColors.Red}Error: Model not found.");
            caller?.PrintToChat($" {selectedModelName} Error: Model not found.");
            return;
        }

        PlayerCredentials player = playerList.FirstOrDefault(p => p.player == caller);
        if (player == null)
        {
            caller?.PrintToChat($" {ChatColors.Red}Error: Player not found.");
            return;
        }

        var modelMenu = new ChatMenu($"{prefix} - {ChatColors.Purple}| {selectedModel.Modelname} |");

        if (player.OwnedModels.Contains(selectedModel.Modelid))
        {
            if (player.WornModelCT == selectedModel.Modelid || player.WornModelT == selectedModel.Modelid)
            {
                modelMenu.AddMenuOption($" {ChatColors.Red} Unequip this model", (caller, _) =>
                {
                    if (selectedModel.AllowedTeam == "CT")
                    {
                        player.WornModelCT = -1;
                    }
                    else if (selectedModel.AllowedTeam == "T")
                    {
                        player.WornModelT = -1;
                    }
                    caller?.PrintToChat($"{prefix} {ChatColors.Red}You have unequiped the model.");
                });
                modelMenu.AddMenuOption($" {ChatColors.Red} Remove this model", (caller, _) =>
                {
                    if (selectedModel.AllowedTeam == "CT")
                    {
                        player.WornModelCT = -1;
                        player.OwnedModels.Remove(selectedModel.Modelid);
                        RemoveProduct(player.SteamId, selectedModel.Modelid);
                    }
                    else if (selectedModel.AllowedTeam == "T")
                    {
                        player.WornModelT = -1;
                        player.OwnedModels.Remove(selectedModel.Modelid);
                        RemoveProduct(player.SteamId, selectedModel.Modelid);
                    }
                    caller?.PrintToChat($"{prefix} {ChatColors.Red}You have removed the model.");
                });
                modelMenu.AddMenuOption($" {ChatColors.Red} Sell this model", (caller, _) =>
                {
                    if (selectedModel.AllowedTeam == "CT")
                    {
                        player.WornModelCT = -1;
                        SellModel(caller, selectedModel);
                    }
                    else if (selectedModel.AllowedTeam == "T")
                    {
                        player.WornModelT = -1;
                        SellModel(caller, selectedModel);
                    }
                });
            }
            else
            {

                modelMenu.AddMenuOption($" {ChatColors.Green}Equip this model", (caller, _) =>
                {
                    if (selectedModel.AllowedTeam == "CT")
                    {
                        player.WornModelCT = selectedModel.Modelid;
                    }
                    else if (selectedModel.AllowedTeam == "T")
                    {
                        player.WornModelT = selectedModel.Modelid;
                    }
                    caller?.PrintToChat($"{prefix} {ChatColors.Green}You are now using the model. It will be available after you respawn!");
                });
                modelMenu.AddMenuOption($" {ChatColors.Red} Remove this model", (caller, _) =>
                {
                    if (selectedModel.AllowedTeam == "CT")
                    {
                        player.WornModelCT = -1;
                        player.OwnedModels.Remove(selectedModel.Modelid);
                        RemoveProduct(player.SteamId, selectedModel.Modelid);
                    }
                    else if (selectedModel.AllowedTeam == "T")
                    {
                        player.WornModelT = -1;
                        player.OwnedModels.Remove(selectedModel.Modelid);
                        RemoveProduct(player.SteamId, selectedModel.Modelid);
                    }
                    caller?.PrintToChat($"{prefix} {ChatColors.Red}You have removed the model.");
                });
                modelMenu.AddMenuOption($" {ChatColors.Red} Sell this model", (caller, _) =>
                {
                    if (selectedModel.AllowedTeam == "CT")
                    {
                        player.WornModelCT = -1;
                        SellModel(caller, selectedModel);
                    }
                    else if (selectedModel.AllowedTeam == "T")
                    {
                        player.WornModelT = -1;
                        SellModel(caller, selectedModel);
                    }
                });
            }
        }
        else
        {
            modelMenu.AddMenuOption($" {ChatColors.Lime}Buy this model for {ChatColors.Red}{selectedModel.Price} Credits", (caller, _) =>
            {
                if (GetPlayerBalance(caller) < selectedModel.Price) return;
                PurchaseModel(caller, selectedModel);
                caller?.PrintToChat($"{prefix} {ChatColors.Green}Model purchased! You now own {selectedModel.Modelname}. Equip it in your inventory to use it!");
                return;
            });
        }
        ChatMenus.OpenMenu(caller, modelMenu);
    }

    [ConsoleCommand("css_env", "Opens the inventory menu")]
    [ConsoleCommand("css_inventory", "Opens the inventory menu")]
    [ConsoleCommand("css_inv", "Opens the inventory menu")]
    public void OnInventoryCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var title = $"{prefix} - {ChatColors.Purple}| Inventory |";
        var storeMenu = new ChatMenu(title);
        storeMenu.AddMenuOption($" {ChatColors.Blue}CT Models", InvModelMenu);
        storeMenu.AddMenuOption($" {ChatColors.Gold}T Models", InvModelMenu);

        ChatMenus.OpenMenu(caller, storeMenu);
    }

    private void InvModelMenu(CCSPlayerController? caller, ChatMenuOption option)
    {
        PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == caller);
        string ct = $" {ChatColors.Blue}CT Models";
        string t = $" {ChatColors.Gold}T Models";

        if (option.Text == ct)
        {
            var titlect = $"{prefix} - {ChatColors.Purple}| Store CT Models |";
            var modelMenuCT = new ChatMenu(titlect);
            foreach (int modelid in Player.OwnedModels)
            {
                var selectedModel = models.FirstOrDefault(m => m.Modelid == modelid);
                if (Player.WornModelCT == modelid)
                {
                    modelMenuCT.AddMenuOption($"{selectedModel.Modelname} - {ChatColors.Green}[USING]", SelectedModelMenu);
                }
                else if (selectedModel.AllowedTeam == "CT")
                {
                    modelMenuCT.AddMenuOption($"{selectedModel.Modelname} - {ChatColors.Gold}[OWNED]", SelectedModelMenu);
                }
                else
                {
                    return;
                }
            }

            ChatMenus.OpenMenu(caller, modelMenuCT);
        }
        else if (option.Text == t)
        {
            var titlet = $"{prefix} - {ChatColors.Purple}| Store T Models |";
            var modelMenuT = new ChatMenu(titlet);
            foreach (int modelid in Player.OwnedModels)
            {
                var selectedModel = models.FirstOrDefault(m => m.Modelid == modelid);
                if (Player.WornModelT == modelid)
                {
                    modelMenuT.AddMenuOption($"{selectedModel.Modelname} - {ChatColors.Green}[USING]", SelectedModelMenu);
                }
                else if (selectedModel.AllowedTeam == "T")
                {
                    modelMenuT.AddMenuOption($"{selectedModel.Modelname} - {ChatColors.Gold}[OWNED]", SelectedModelMenu);
                }
                else
                {
                    return;
                }
            }

            ChatMenus.OpenMenu(caller, modelMenuT);
        }
    }
}