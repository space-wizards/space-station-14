using System.IO;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.MapRenderer;

/// <summary>
/// A single target map that the map renderer should render.
/// </summary>
/// <seealso cref="RenderMapPrototype"/>
/// <seealso cref="RenderMapFile"/>
public abstract class RenderMap
{
    /// <summary>
    /// Short identifier of the map that should be unique-ish. Used in file names and other important stuff.
    /// </summary>
    public abstract string ShortName { get; }
}

/// <summary>
/// Specifies a map prototype that the map renderer should render.
/// </summary>
public sealed class RenderMapPrototype : RenderMap
{
    /// <summary>
    /// The ID of the prototype to render.
    /// </summary>
    public required ProtoId<GameMapPrototype> Prototype;

    public override string ShortName => Prototype;

    public override string ToString()
    {
        return $"{nameof(RenderMapPrototype)}({Prototype})";
    }
}

/// <summary>
/// Specifies a map file on disk that the map renderer should render.
/// </summary>
public sealed class RenderMapFile : RenderMap
{
    /// <summary>
    /// The path to the file that should be rendered. This is an OS disk path, *not* a <see cref="ResPath"/>.
    /// </summary>
    public required string FileName;

    public override string ShortName => Path.GetFileNameWithoutExtension(FileName);

    public override string ToString()
    {
        return $"{nameof(RenderMapFile)}({FileName})";
    }
}
