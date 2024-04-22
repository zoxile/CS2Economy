using Dapper;
using MySqlConnector;
using System;
using System.Data;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Economy;

public partial class CS2Economy
{
    public async Task<MySqlConnection> GetConnectionAsync()
    {
        try
        {
            MySqlConnection? database = new MySqlConnection($"Server={Config.DatabaseHost};User ID={Config.DatabaseUser};Password={Config.DatabasePassword};Database={Config.DatabaseName};Port={Config.DatabasePort}");
            await database.OpenAsync();

            return database;
        }

        catch
        {
            Console.WriteLine("[CS2Economy] Could not connect to the database!");
            return null;
        }
    }

    public static async Task SetupTable(MySqlConnection? database)
    {
        if (database == null)
        {
            Console.WriteLine("[CS2Economy] Database connection is null.");
            return;
        }

        try
        {
            var createPlayerTable = @"
            CREATE TABLE IF NOT EXISTS `cs2economy_players` (
                `playername` VARCHAR(128) NOT NULL,
                `steamid` VARCHAR(32) PRIMARY KEY NOT NULL,
                `lastwornct` INT(10) NOT NULL,
                `lastwornt` INT(10) NOT NULL,
                `credits` INT(64) NOT NULL
            );";

            var createModelTable = @"
              CREATE TABLE IF NOT EXISTS `cs2economy_forsale` (
                  `steamid` VARCHAR(32) NOT NULL,
                  `playername` VARCHAR(64) NOT NULL,
                  `modelid` INT(10) PRIMARY KEY NOT NULL,
                  `modelname` VARCHAR(64) NOT NULL,
                  `price` INT(12) NOT NULL
            );";

            var createItemsTable = @"
              CREATE TABLE IF NOT EXISTS `cs2economy_products` (
                  `playername` VARCHAR(64) NOT NULL,
                  `steamid` VARCHAR(32) PRIMARY KEY NOT NULL,
                  `modelid` INT(10) NOT NULL,
                  `purchased_price` INT(12) NOT NULL
                );";

            await database.ExecuteAsync(createPlayerTable);
            await database.ExecuteAsync(createModelTable);
            await database.ExecuteAsync(createItemsTable);

            Console.WriteLine("[CS2Economy] Tables created successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[CS2Economy] Error creating tables: {e.Message}");
        }
    }


    public async Task SetupPlayer(string steamID, string playerName)
    {
        var database = await GetConnectionAsync();

        if (database == null)
        {
            return;
        }

        using var insertPlayer = new MySqlCommand("INSERT IGNORE INTO cs2economy_players (steamid, playername) VALUES (@steamID, @playerName)", database);
        insertPlayer.Parameters.AddWithValue("@steamID", steamID);
        insertPlayer.Parameters.AddWithValue("@playerName", playerName);

        try
        {
            await insertPlayer.ExecuteNonQueryAsync();
            Console.WriteLine($"[CS2Economy] Player {playerName} with SteamID {steamID} added successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CS2Economy] Error setting up player: {ex.Message}");
        }
    }


    public async void UpdateCredits(string playerName, string steamID, int credits)
    {
        var database = await GetConnectionAsync();

        if (database == null || credits < 0)
        {
            return;
        }

        try
        {
            using (database)
            {
                var query = "UPDATE cs2economy_players SET credits = @credits WHERE steamid = @steamid";

                using (var command = new MySqlCommand(query, database))
                {
                    command.Parameters.AddWithValue("@credits", credits);
                    command.Parameters.AddWithValue("@steamid", steamID);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task AddCredits(string steamID, int amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("[CS2Economy] Invalid credit amount.");
            return;
        }

        var database = await GetConnectionAsync();

        if (database == null)
        {
            Console.WriteLine("[CS2Economy] Database connection is null.");
            return;
        }

        try
        {
            var query = "UPDATE cs2economy_players SET credits = credits + @amount WHERE steamid = @steamid";

            using (var command = new MySqlCommand(query, database))
            {
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@steamid", steamID);

                await command.ExecuteNonQueryAsync();

                Console.WriteLine($"[CS2Economy] Added {amount} credits to player with SteamID {steamID}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CS2Economy] Error adding credits: {ex.Message}");
        }
    }

    public async Task RemoveCredits(string steamID, int amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("[CS2Economy] Invalid credit amount.");
            return;
        }

        var database = await GetConnectionAsync();

        if (database == null)
        {
            Console.WriteLine("[CS2Economy] Database connection is null.");
            return;
        }

        try
        {
            var query = "UPDATE cs2economy_players SET credits = credits - @amount WHERE steamid = @steamid AND credits >= @amount";

            using (var command = new MySqlCommand(query, database))
            {
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@steamid", steamID);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"[CS2Economy] Removed {amount} credits from player with SteamID {steamID}.");
                }
                else
                {
                    Console.WriteLine("[CS2Economy] Not enough credits to remove.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CS2Economy] Error removing credits: {ex.Message}");
        }
    }


    public async Task UpdateProduct(string playerName, string steamID, int modelID, int purchasedPrice)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var query = @"INSERT INTO cs2economy_products (playername, steamid, modelid, purchased_price)
                          VALUES (@playername, @steamid, @modelid, @purchasedPrice)
                          ON DUPLICATE KEY UPDATE 
                              playername = VALUES(playername), 
                              steamid = VALUES(steamid), 
                              purchased_price = VALUES(purchased_price)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@playername", playerName);
                    command.Parameters.AddWithValue("@steamid", steamID);
                    command.Parameters.AddWithValue("@modelid", modelID);
                    command.Parameters.AddWithValue("@purchasedPrice", purchasedPrice);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task<int?> GetPurchasedPrice(string steamID, int modelID)
    {
        int? purchasedPrice = null;

        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var query = @"SELECT purchased_price 
                          FROM cs2economy_products 
                          WHERE steamid = @steamid AND modelid = @modelID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@steamid", steamID);
                    command.Parameters.AddWithValue("@modelid", modelID);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        purchasedPrice = Convert.ToInt32(result);
                    }
                    else
                    {
                        Console.WriteLine($"No purchased price found for ModelID: {modelID} and SteamID: {steamID}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return purchasedPrice;
    }

    public async Task UpdateWornModel(string steamID, int wornModelT, int wornModelCT)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var query = @"UPDATE cs2economy_players 
                          SET lastwornct = @wornModelCT, lastwornt = @wornModelT
                          WHERE steamid = @steamID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@wornModelCT", wornModelCT);
                    command.Parameters.AddWithValue("@wornModelT", wornModelT);
                    command.Parameters.AddWithValue("@steamID", steamID);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task RemoveProduct(string steamID, int modelID)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var query = @"DELETE FROM cs2economy_products 
                          WHERE steamid = @steamid AND modelID = @modelID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@steamid", steamID);
                    command.Parameters.AddWithValue("@modelID", modelID);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task UpdatePlayerWornModelsFromDatabase(string steamId)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var query = @"SELECT lastwornct, lastwornt 
                          FROM cs2economy_players 
                          WHERE steamid = @steamid";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@steamid", steamId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            PlayerCredentials player = playerList.FirstOrDefault(p => p.SteamId == steamId);

                            if (player != null)
                            {
                                if (player.OwnedModels.Contains(reader.IsDBNull("lastwornct") ? -1 : reader.GetInt32("lastwornct")))
                                {
                                    player.WornModelCT = reader.IsDBNull("lastwornct") ? -1 : reader.GetInt32("lastwornct");
                                }
                                else if (player.OwnedModels.Contains(reader.IsDBNull("lastwornt") ? -1 : reader.GetInt32("lastwornt")))
                                {
                                    player.WornModelT = reader.IsDBNull("lastwornt") ? -1 : reader.GetInt32("lastwornt");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    public async Task UpdateOwnedModelsFromDatabase(string steamID)
    {
        var ownedModels = new List<int>();

        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var query = @"SELECT modelid 
                      FROM cs2economy_products 
                      WHERE steamid = @steamid";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@steamid", steamID);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var modelID = reader.GetInt32("modelid");
                            ownedModels.Add(modelID);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        PlayerCredentials player = playerList.FirstOrDefault(p => p.SteamId == steamID);
        if (player != null)
        {
            var newModels = ownedModels.Except(player.OwnedModels).ToList();
            player.OwnedModels.AddRange(newModels);
        }
    }


    public async Task<PlayerDataFromDB> GetPlayerDataFromDB(string steamId)
    {
        using (var connection = await GetConnectionAsync())
        {
            var query = "SELECT * FROM cs2economy_players WHERE steamid = @SteamId";
            var parameters = new { SteamId = steamId };

            var result = await connection.QueryFirstOrDefaultAsync<PlayerDataFromDB>(query, parameters);

            return result;
        }
    }

    public class PlayerDataFromDB
    {
        public string playername { get; set; }
        public string steamid { get; set; }
        public int lastwornct { get; set; }
        public int lastwornt { get; set; }
        public int credits { get; set; }
    }
}