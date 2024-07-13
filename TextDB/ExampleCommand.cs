using System;
using CommandSystem;

namespace TextDB;

[CommandHandler(typeof(GameConsoleCommandHandler))] //Uncomment this line if you want to use this command in the game
public class ExampleCommand : ICommand, IUsageProvider
{
    public string Command => "TextDatabaseTest";
    public string Description => $"{nameof(TextDatabase)} test command";
    public string[] Aliases { get; } = ["Tdb"];
    public string[] Usage { get; } = ["set|add|remove|read", "arguments"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        //Can also be stored in a static field or property, that's just an example
        var db = TextDatabase.Open("Test");

        if (arguments.Count < 2)
        {
            response = "This command requires at least 2 arguments";
        }

        switch (arguments.At(0).ToLower())
        {
            case "add":
                db.Add(arguments.At(1), arguments.At(2));
                break;
            case "set":
                db[arguments.At(1)] = arguments.At(2);
                break;
            case "remove":
                db.Remove(arguments.At(1));
                break;
            case "read":
                response = db[arguments.At(1)];
                return false;
            default:
                response = "Invalid operation";
                return false;
        }

        response = "Done";
        return true;
    }
}