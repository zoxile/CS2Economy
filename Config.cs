using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using System.IO;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CS2Economy
{
	public partial class CS2Economy
	{
		public static List<Models> models = new List<Models>();
		public static List<PlayerCredentials> playerList = new List<PlayerCredentials>();
	}

	public class CS2EconomyConfig : BasePluginConfig
	{
		[JsonPropertyName("Prefix")]
		public string Prefix { get; set; } = "[CS2Economy]";

		[JsonPropertyName("DatabaseHost")]
		public string DatabaseHost { get; set; } = "";

		[JsonPropertyName("DatabasePort")]
		public int DatabasePort { get; set; } = 3306;

		[JsonPropertyName("DatabaseUser")]
		public string DatabaseUser { get; set; } = "";

		[JsonPropertyName("DatabasePassword")]
		public string DatabasePassword { get; set; } = "";

		[JsonPropertyName("DatabaseName")]
		public string DatabaseName { get; set; } = "";

		[JsonPropertyName("EnableModels")]
		public bool EnableModels { get; set; } = true;

		[JsonPropertyName("EnableDefaultModels")]
		public bool EnableDefaultModels { get; set; } = false;

		[JsonPropertyName("EnableGambling")]
		public bool EnableGambling { get; set; } = true;

		[JsonPropertyName("EnableRewards")]
		public bool EnableRewards { get; set; } = true;

		[JsonPropertyName("CreditReward")]
		public int CreditReward { get; set; } = 15;

		[JsonPropertyName("PlusRewardVIP")]
		public bool PlusRewardVIP { get; set; } = true;

		[JsonPropertyName("IntervalMinutes")]
		public int IntervalMinutes { get; set; } = 5;

		[JsonPropertyName("gamblingIntervalMinutes")]
		public int gamblingIntervalMinutes { get; set; } = 1;

		[JsonPropertyName("ModuleDirectory")]
		public string moduleDirectory { get; set; } = "/home/container/game/csgo/addons/counterstrikesharp/plugins/CS2Economy";

	}

	public partial class CS2Economy
	{

		public static void CreateModelsList()
		{
			var configPath = Path.Combine(modulePath, "models.json");

			if (!File.Exists(configPath))
			{
				var models = new
				{
					CS2Economy = new
					{
						TModels = new List<dynamic>
				{
					new
					{
						modelname = "Name1",
						modelid = 10,
						modelpath = "characters\\path\\modelt.vmdl",
						price = 10000,
						team = "T"
					}
				},
						CTModels = new List<dynamic>
				{
					new
					{
						modelname = "Name2",
						modelid = 20,
						modelpath = "characters\\path\\modelct.vmdl",
						price = 10000,
						team = "CT"
					}
				}
					}
				};

				string jsonString = JsonSerializer.Serialize(models, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(configPath, jsonString);

				Console.WriteLine($"[CS2Economy] JSON file '{configPath}' created successfully.");
			}
			else
			{
				Console.WriteLine($"[CS2Economy] JSON file '{configPath}' already exists.");
			}

			LoadModels();
		}

		public static void LoadModels()
		{
			try
			{
				string filepath = $"{modulePath}/models.json";
				var json = File.ReadAllText(filepath);
				var modelsData = JsonSerializer.Deserialize<JsonElement>(json);

				if (modelsData.TryGetProperty("CS2Economy", out JsonElement cs2Economy))
				{
					if (cs2Economy.TryGetProperty("TModels", out JsonElement tModels))
					{
						foreach (var model in tModels.EnumerateArray())
						{
							var newModel = new Models(
								model.GetProperty("modelname").GetString(),
								model.GetProperty("modelid").GetInt32(),
								model.GetProperty("modelpath").GetString(),
								model.GetProperty("price").GetInt32(),
								model.GetProperty("team").GetString());

							models.Add(newModel);

							Console.WriteLine($"Model Loaded: {newModel.Modelname}, ID: {newModel.Modelid}, Path: {newModel.ModelPath}, Price: {newModel.Price}, Team: {newModel.AllowedTeam}");
						}
					}

					if (cs2Economy.TryGetProperty("CTModels", out JsonElement ctModels))
					{
						foreach (var model in ctModels.EnumerateArray())
						{
							var newModel = new Models(
								model.GetProperty("modelname").GetString(),
								model.GetProperty("modelid").GetInt32(),
								model.GetProperty("modelpath").GetString(),
								model.GetProperty("price").GetInt32(),
								model.GetProperty("team").GetString());

							models.Add(newModel);

							Console.WriteLine($"Model Loaded: {newModel.Modelname}, ID: {newModel.Modelid}, Path: {newModel.ModelPath}, Price: {newModel.Price}, Team: {newModel.AllowedTeam}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading models: {ex.Message}");
			}
		}

		public static void AddModelToList(string modelName, int modelId, string modelPath, int price, string allowedTeam)
		{
			models.Add(new Models(modelName, modelId, modelPath, price, allowedTeam));
		}

		static string GetStringProperty(JsonElement element, string propertyName)
		{
			return element.TryGetProperty(propertyName, out var property) ? property.GetString() : string.Empty;
		}

		static int GetInt32Property(JsonElement element, string propertyName)
		{
			return element.TryGetProperty(propertyName, out var property) ? property.GetInt32() : 0;
		}
	}

	/*
	public partial class CS2Economy
	{
		private FileSystemWatcher _fileWatcher;
		private string _filePath;

		public ModelsWatcher(string filePath)
		{
			_filePath = filePath;
			SetupWatcher();
			return;
		}

		private void SetupWatcher()
		{
			var directory = Path.GetDirectoryName(_filePath);
			var fileName = Path.GetFileName(_filePath);

			_fileWatcher = new FileSystemWatcher(directory, fileName)
			{
				NotifyFilter = NotifyFilters.LastWrite
			};

			_fileWatcher.Changed += OnFileChanged;
			_fileWatcher.EnableRaisingEvents = true;
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine($"Detected changes in {e.FullPath}. Reloading models...");

			System.Threading.Thread.Sleep(500);

			try
			{

				LoadModels();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to reload models: {ex.Message}");
			}
		}
	}
	*/
}