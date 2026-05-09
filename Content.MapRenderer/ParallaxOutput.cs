using System.Collections.Generic;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.MapRenderer;

/// <summary>
/// Helper class for collecting the files used for parallax output
/// </summary>
public sealed class ParallaxOutput
{
    public const string OutputDirectory = "_parallax";

    public readonly HashSet<ResPath> FilesToCopy = [];

    private readonly string _outputPath;

    /// <summary>
    /// Helper class for collecting the files used for parallax output
    /// </summary>
    public ParallaxOutput(string outputPath)
    {
        _outputPath = outputPath;
        Directory.CreateDirectory(Path.Combine(_outputPath, OutputDirectory));
    }

    public string ReferenceResourceFile(IResourceManager resourceManager, ResPath path)
    {
        var fileName = Path.Combine(OutputDirectory, path.Filename);
        if (FilesToCopy.Add(path))
        {
            using var file = resourceManager.ContentFileRead(path);
            using var target = File.Create(Path.Combine(_outputPath, fileName));

            file.CopyTo(target);
        }

        return fileName;
    }
}
