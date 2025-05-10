using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.MapRenderer.Extensions;

namespace Content.MapRenderer;

public sealed class CommandLineArguments
{
    public List<string> Maps { get; set; } = new();
    public OutputFormat Format { get; set; } = OutputFormat.png;
    public bool ExportViewerJson { get; set; } = false;
    public string OutputPath { get; set; } = DirectoryExtensions.MapImages().FullName;
    public bool ArgumentsAreFileNames { get; set; } = false;
    public bool ShowMarkers { get; set; } = false;

    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArguments? parsed)
    {
        parsed = new CommandLineArguments();

        if (args.Count == 0)
        {
            PrintHelp();
            //Returns true here so the user can select what maps they want to render
            return true;
        }

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

                case "-o":
                case "--output":
                    enumerator.MoveNext();
                    parsed.OutputPath = enumerator.Current;
                    break;

                case "-f":
                case "--files":
                    parsed.ArgumentsAreFileNames = true;
                    break;

                case "-m":
                case "--markers":
                    parsed.ShowMarkers = true;
                    break;

                case "-h":
                case "--help":
                    PrintHelp();
                    return false;

                default:
                    parsed.Maps.Add(argument);
                    break;
            }
        }

        if (parsed.ArgumentsAreFileNames && parsed.Maps.Count == 0)
        {
            Console.WriteLine("No file names specified!");
            PrintHelp();
            return false;
        }

        return true;
    }

    public static void PrintHelp()
    {
        Console.WriteLine(@"Content.MapRenderer <options> [map names]
Options:
    --format <png|webp>
        Specifies the format the map images will be exported as.
        Defaults to: png
    --viewer
        Causes the map renderer to create the map.json files required for use with the map viewer.
        Also puts the maps in the required directory structure.
    -o / --output <output path>
        Changes the path the rendered maps will get saved to.
        Defaults to Resources/MapImages
    -f / --files
        This option tells the map renderer that you supplied a list of map file names instead of their ids.
        Example: Content.MapRenderer -f /Maps/box.yml /Maps/bagel.yml
    -m / --markers
        Show hidden markers on map render. Defaults to false.
    -h / --help
        Displays this help text");
    }
}

public sealed class CommandLineArgumentException : Exception
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
