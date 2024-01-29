using Content.Client.Chemistry.Visualizers;

namespace Content.Client.Storage.Components;

/// <summary>
///     Essentially a version of <see cref="SolutionContainerVisualsComponent"/> fill level handling but for item storage.
///     Depending on the fraction of storage that's filled, will change the sprite at <see cref="FillLayer"/> to the nearest
///     fill level, up to <see cref="MaxFillLevels"/>.
/// </summary>
[RegisterComponent]
public sealed partial class StorageContainerVisualsComponent : Component
{
    [DataField("maxFillLevels")]
    public int MaxFillLevels = 0;

    /// <summary>
    ///     A prefix to use for the fill states., i.e. {FillBaseName}{fill level} for the state
    /// </summary>
    [DataField("fillBaseName")]
    public string? FillBaseName;

    [DataField("layer")]
    public StorageContainerVisualLayers FillLayer = StorageContainerVisualLayers.Fill;
}

public enum StorageContainerVisualLayers : byte
{
    Fill
}
