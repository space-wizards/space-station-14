using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Content.MapRenderer;

public sealed class CommandLineArguments
{
    public List<string> Maps { get; set; } = new();
    public OutputFormat Format { get; set; } = OutputFormat.png;
    public bool ExportViewerJson { get; set; } = false;

    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArguments? parsed)
    {
        parsed = new CommandLineArguments();

        using var enumerator = args.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var argument = enumerator.Current;
            switch (argument)
            {
                case "--format":
                    enumerator.MoveNext();

                    if (!Enum.TryParse<OutputFormat>(enumerator.Current, out var format))
                    {
                        Console.WriteLine("Invalid format specified for option: {0}", argument);
                        parsed = null;
                        return false;
                    }

                    parsed.Format = format;
                    break;

                case "--viewer":
                    parsed.ExportViewerJson = true;
                    break;

                default:
                    parsed.Maps.Add(argument);
                    break;
            }
        }

        return true;
    }
}

public class CommandLineArgumentException : Exception
{
    public CommandLineArgumentException(string? message) : base(message)
    {
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum OutputFormat
{
    png,
    webp
}
