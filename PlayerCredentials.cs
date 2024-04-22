using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CS2Economy
{
    public class Models
    {
        public string Modelname { get; set; }
        public int Modelid { get; set; }
        public string ModelPath { get; set; }
        public int Price { get; set; }
        public string AllowedTeam { get; set; }

        public Models(string modelName, int modelId, string modelPath, int price, string allowedTeam)
        {
            Modelname = modelName;
            Modelid = modelId;
            ModelPath = modelPath;
            Price = price;
            AllowedTeam = allowedTeam;
        }
    }

    public class PlayerCredentials
    {
        public CCSPlayerController player { get; set; }
        public int? UserID { get; set; }
        public string Name { get; set; }
        public string SteamId { get; set; }
        public int Balance { get; set; }
        public List<int> OwnedModels { get; set; }
        public int WornModelT { get; set; }
        public int WornModelCT { get; set; }

        public PlayerCredentials(CCSPlayerController Player, int? userId, string playerName, string steamId)
        {
            player = Player;
            UserID = userId;
            Name = playerName;
            SteamId = steamId;
            Balance = 0;
            OwnedModels = new List<int>();
            WornModelT = -1;
            WornModelCT = -1;
        }
    }
}
