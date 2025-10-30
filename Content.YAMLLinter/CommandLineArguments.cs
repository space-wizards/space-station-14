using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace Content.YAMLLinter;

public sealed class CommandLineArguments
{
    public bool Diff { get; set; } = false;
    public string DiffPath { get; set; } = "/Prototypes";
    public bool DiffIncludeAbstract { get; set; } = false;

    // TODO rename this shit
    public bool Diff2 { get; set; } = false;
    public string Diff2PathBefore { get; set; } = null;
    public string Diff2PathAfter { get; set; } = null;

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
                    // optionally include abstract protos
                    if (enumerator.MoveNext() && enumerator.Current == "true")
                    {
                        parsed.DiffIncludeAbstract = true; // probably a cleaner way to do this but idgaf!
                    }
                    break;

                case "--diff2":
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided for before state!");
                        parsed = null;
                        return false;
                    }
                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("No file path provided for after state!");
                        parsed = null;
                        return false;
                    }
                    parsed.Diff2PathAfter = enumerator.Current;
                    parsed.Diff2 = true;
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
