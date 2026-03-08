using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace Content.YAMLLinter;

public sealed class CommandLineArguments
{
    public bool Save { get; set; } = false;
    public string SavePath { get; set; } = "/Prototypes";
    public bool SaveIncludeAbstract { get; set; } = false;

    public bool Diff { get; set; } = false;
    public string DiffPathBefore { get; set; } = "/entity-prototypes.yml";

    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArguments parsed)
    {
        parsed = new CommandLineArguments();

        if (args.Count == 0)
        {
            return true;
        }

        using var enumerator = args.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var argument = enumerator.Current;
            switch (argument)
            {
                case "-help": // is there a convention for this that doesnt get dotnet???
                    PrintHelp();
                    return false;
                case "--save":
                    parsed.Save = true;
                    break;
                case "--diff":
                    parsed.Diff = true;
                    break;

                // optional args
                case "-path":
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided for prototypes to save!");
                        break;
                    }
                    parsed.SavePath = enumerator.Current;
                    break;
                case "-before":
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided for diff before state!");
                        break;
                    }
                    parsed.DiffPathBefore = enumerator.Current;
                    break;
                case "-abstract":
                    parsed.SaveIncludeAbstract = true;
                    break;


                default:
                    if (argument.StartsWith('-'))
                    {
                        Console.WriteLine($"Unknown argument: {argument}");
                        return false;
                    }
                    break;
            }
        }
        return true;
    }
    private static void PrintHelp()
    {
        Console.WriteLine(@"
Usage: Content.YAMLLinter [options]

Options:
  -help             Show this help text.

  --save            Save a list of entity prototypes in specified directory. Output file will be in root folder.
  --diff            Generate a differential between a provided before-state of entities and entities in working tree.

  [-path]           Declare directory of prototypes to save from. Will use all directories if unspecified.
  [-before]         Declare differential before-state. Will use default save output path if unspecified.
  [-abstract]       Include abstract prototypes.
");
    }
}
