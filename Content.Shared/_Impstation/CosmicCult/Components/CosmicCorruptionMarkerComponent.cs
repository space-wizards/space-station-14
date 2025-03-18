using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// This is used to mark an entity as corruptible by CosmicCorruptingSystem.
/// currently unused because I'm acutally not sure if / how I want to use it.
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCorruptionMarkerComponent : Component
{
    [DataField]
    public EntProtoId ConvertTo = default;
}
