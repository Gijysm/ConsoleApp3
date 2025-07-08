using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

public class AppConfig
{
    [JsonPropertyName("ConnectionString")]
    public string? ConnectionString { get; set; }

    [JsonPropertyName("param1")]
    public string? Param1 { get; set; }

    [JsonPropertyName("param2")]
    public bool Param2 { get; set; }
}

public class Parser
{
    public AppConfig? ParseJs(string js)
    {
        string jsonText = File.ReadAllText(js);
        return JsonSerializer.Deserialize<AppConfig>(jsonText);
    }

    public void AddToBase(SqliteConnection connection, AppConfig config)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText =
            @"
INSERT OR IGNORE INTO Users (Name, param2)
VALUES ($name, $param2);
";
        insertCommand.Parameters.AddWithValue("$name", config.Param1 ?? "NoName");
        insertCommand.Parameters.AddWithValue("$param2", config.Param2 ? 1 : 0);
        insertCommand.ExecuteNonQuery();
    }

    public void ReadBase(SqliteConnection connection)
    {
        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT * FROM Users ORDER BY Id;";
        using var reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            bool param2 = reader.GetBoolean(2);

            Console.WriteLine($"Id: {id}");
            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"param2: {param2}");
            Console.WriteLine("-------------------");
        }
    }
}

internal class Program
{
    public static void Main(string[] args)
    {
        Parser parser = new();
        var config = parser.ParseJs("appsettings.json");

        if (config == null || string.IsNullOrEmpty(config.ConnectionString))
        {
            Console.WriteLine("Помилка: Неможливо зчитати конфігурацію або відсутній рядок підключення.");
            return;
        }

        SQLitePCL.Batteries.Init();
        using var connection = new SqliteConnection(config.ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    param2 BOOLEAN NOT NULL
);";
        command.ExecuteNonQuery();

        var clearCmd = connection.CreateCommand();
        clearCmd.CommandText = "DELETE FROM Users;";
        clearCmd.ExecuteNonQuery();

        var resetSeqCmd = connection.CreateCommand();
        resetSeqCmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='Users';";
        resetSeqCmd.ExecuteNonQuery();

        parser.AddToBase(connection, config);
        parser.ReadBase(connection);
    }
}
