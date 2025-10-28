using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace Content.YAMLLinter;

public sealed class CommandLineArguments
{
    public bool Diff { get; set; } = false;
    public string DiffPath { get; set; } = "/Prototypes";

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
                case "--diff":
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided!");
                        parsed = null;
                        return false;
                    }
                    parsed.Diff = true;
                    parsed.DiffPath = enumerator.Current;
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
}
