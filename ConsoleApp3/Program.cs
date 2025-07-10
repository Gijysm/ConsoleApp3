using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Data.SqlClient;

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

    public void AddToBase(SqlConnection connection, AppConfig config)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText =
            @"
IF NOT EXISTS (SELECT 1 FROM Users WHERE Name = @name)
BEGIN
    INSERT INTO Users (Name, param2)
    VALUES (@name, @param2)
END
";
        insertCommand.Parameters.AddWithValue("@name", config.Param1 ?? "NoName");
        insertCommand.Parameters.AddWithValue("@param2", config.Param2);
        insertCommand.ExecuteNonQuery();
    }

    public void ReadBase(SqlConnection connection)
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

        string connectionString = "***Вставити свій рядок підключення***";
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE,
        param2 BIT NOT NULL
    )
END";
        command.ExecuteNonQuery();

        var clearCmd = connection.CreateCommand();
        clearCmd.CommandText = "DELETE FROM Users;";
        clearCmd.ExecuteNonQuery();
        
        parser.AddToBase(connection, config);
        parser.ReadBase(connection);

        parser.AddToBase(connection, config);
        parser.ReadBase(connection);
    }
}
