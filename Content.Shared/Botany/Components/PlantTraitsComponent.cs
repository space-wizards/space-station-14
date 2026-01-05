using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for managing special plant traits and mutations.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantTraitsComponent : Component
{
    [DataField]
    public List<PlantTrait> Traits = [];
}
