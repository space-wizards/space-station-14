using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace Content.YAMLLinter;

public sealed class CommandLineArguments
{
    // TODO docs
    public bool Save { get; set; } = false;
    public string SavePath { get; set; } = "/Prototypes";
    public bool SaveIncludeAbstract { get; set; } = false;

    public bool Diff { get; set; } = false;
    public string DiffPathBefore { get; set; } = null;

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
                case "--save":
                    if (enumerator.MoveNext())
                    {
                        parsed.SavePath = enumerator.Current;
                    }
                    parsed.Save = true;
                    // optionally include abstract protos
                    if (enumerator.MoveNext() && enumerator.Current == "true")
                    {
                        parsed.SaveIncludeAbstract = true; // probably a cleaner way to do this but idgaf!
                    }
                    break;

                case "--diff":
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided for before state!");
                        parsed = null;
                        return false;
                    }
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided!");
                        parsed = null;
                        return false;
                    }
                    parsed.SavePath = enumerator.Current;
                    parsed.Diff = true;
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
