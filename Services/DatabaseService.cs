using System.Text.Json;
using SystemRemoteService.Models;

namespace SystemRemoteService.Services;

public class DatabaseService
{
    private const string FilePath = "DataBase/commands.json";

    public static List<Command> LoadCommands()
    {
        if (!File.Exists(FilePath))
        {
            SaveCommands(new List<Command>());
            return new List<Command>();
        }

        var json = File.ReadAllText(FilePath);

        if (string.IsNullOrWhiteSpace(json)) return new List<Command>();

        try
        {
            return JsonSerializer.Deserialize<List<Command>>(json) ?? new List<Command>();
        }
        catch (JsonException)
        {
            return new List<Command>();
        }
    }

    public static void SaveCommands(List<Command> commands)
    {
        var json = JsonSerializer.Serialize(commands, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    public static void AddCommand(Command command)
    {
        var commands = LoadCommands();
        commands.Add(command);
        SaveCommands(commands);
    }

    public static void UpdateCommand(Command command)
    {
        var commands = LoadCommands();
        var existingCommand = commands.FirstOrDefault(c => c.Id == command.Id);
        if (existingCommand != null)
        {
            existingCommand.Prompt = command.Prompt;
            existingCommand.Description = command.Description;
            SaveCommands(commands);
        }
    }

    public static void DeleteCommand(string id)
    {
        var commands = LoadCommands();
        var commandToRemove = commands.FirstOrDefault(c => c.Id == id);
        if (commandToRemove != null)
        {
            commands.Remove(commandToRemove);
            SaveCommands(commands);
        }
    }
}
