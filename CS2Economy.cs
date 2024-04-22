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

[MinimumApiVersion(201)]

public partial class CS2Economy : BasePlugin, IPluginConfig<CS2EconomyConfig>
{
	public override string ModuleName => "CS2 Economy plugin by Zox (thanks to darrenrid)";
	public override string ModuleVersion => "0.0.1";

	public CS2EconomyConfig Config { get; set; } = new CS2EconomyConfig();
	public static string modulePath { get; set; }

	//Default T and CT models for resetting models, temporarily. Paths will be added to config for customization.
	public static readonly string TDefaultModel = "characters\\models\\tm_phoenix\\tm_phoenix.vmdl";
	public static readonly string CTDefaultModel = "characters\\models\\ctm_sas\\ctm_sas.vmdl";

	public override void Load(bool hotReload)
	{
		if (hotReload)
		{
			AddTimer(3, () =>
			{
				Console.WriteLine("[CS2Economy] This plugin does not support hot reload! Restart your server for a stable experience.");
				Server.ExecuteCommand("css_plugins unload CS2Economy");
			});
		}

		AddTimer(20, () =>
		{
			if (Config.EnableGambling)
			{
				int intervalSeconds = (Config.gamblingIntervalMinutes * 60);
				AddTimer(intervalSeconds, StartRoulette, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
			}
			if (Config.EnableRewards)
			{
				int intervalSeconds = (Config.IntervalMinutes * 60);
				AddTimer(intervalSeconds, RewardPlayers, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
			}

		});

		RegisterEvents();
		Console.WriteLine("[CS2Economy] has loaded! WARNING! THIS PLUGIN REQUIRES PRECACHING OF MODELS AND ITEMS!");
		CreateModelsList();
	}

	void SetupDB()
	{
		Task.Run(async () =>
		{
			var database = await GetConnectionAsync();

			await SetupTable(database);
		});
	}

	public void OnConfigParsed(CS2EconomyConfig config)
	{
		if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
		{
			throw new Exception("[CS2Economy] Setup the database credentials in the config file!");
		}
		Config = config;
		modulePath = Config.moduleDirectory;
		prefix = $" {ChatColors.Red}{Config.Prefix}{ChatColors.Default}";

		SetupDB();
	}

	public string GetSteamID(CCSPlayerController player)
	{
		string steamID64 = player?.AuthorizedSteamID?.SteamId64.ToString();

		return steamID64;
	}

	public static TargetResult? GetTarget(CommandInfo command)
	{
		TargetResult targets = command.GetArgTargetResult(1);

		if (!targets.Any())
		{
			command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
			return null;
		}

		if (command.GetArg(1).StartsWith('@'))
			return targets;

		if (targets.Count() == 1)
			return targets;

		command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
		return null;
	}

	public void GivePlayerCredits(CCSPlayerController player, int amount)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
		if (Player != null)
		{
			Player.Balance += amount;
		}
	}

	public void RemovePlayerCredits(CCSPlayerController player, int amount)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
		if (Player != null)
		{
			Player.Balance -= amount;
		}
	}

	public void GivePlayerCreditsOffline(string steamid, int amount)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.SteamId == steamid);
		if (Player != null)
		{
			Player.Balance += amount;
		}
	}

	public void RemovePlayerCreditsOffline(string steamid, int amount)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.SteamId == steamid);
		if (Player != null)
		{
			Player.Balance -= amount;
		}
	}

	public int GetPlayerBalance(CCSPlayerController player)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);

		return Player.Balance;
	}

	public void setPlayerModel(CCSPlayerPawn playerPawn, string model)
	{
		try
		{
			Server.NextFrame(() =>
			{
				playerPawn.SetModel(model);
			});
		}

		catch (Exception ex)
		{
			Console.WriteLine($" {ex.Message.ToString()}");
		}
	}

	public void RemoveAllWornModels()
	{
		foreach (CCSPlayerController player in connectedPlayers.Where(player => player.PawnIsAlive))
		{
			if (player.TeamNum == 3)
			{
				setPlayerModel(player.PlayerPawn.Value, CTDefaultModel);
			}
			else if (player.TeamNum == 2)
			{
				setPlayerModel(player.PlayerPawn.Value, TDefaultModel);
			}
			else
			{
				return;
			}
		}
	}

	public void SellModel(CCSPlayerController player, Models selectedModel)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
		Player.OwnedModels.Remove(selectedModel.Modelid);
		int price = selectedModel.Price;
		GivePlayerCredits(player, price);
		RemoveProduct(Player.SteamId, selectedModel.Modelid);
		player?.PrintToChat($"{prefix} {ChatColors.Red}You have sold this model for {price} Credits!");
	}

	public void PurchaseModel(CCSPlayerController player, Models selectedModel)
	{
		PlayerCredentials Player = playerList.FirstOrDefault(p => p.player == player);
		int price = selectedModel.Price;
		Player.OwnedModels.Add(selectedModel.Modelid);
		RemovePlayerCredits(player, price);
		UpdateProduct(player.PlayerName, Player.SteamId, selectedModel.Modelid, price);
	}
}
