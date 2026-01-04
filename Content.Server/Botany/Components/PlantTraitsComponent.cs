using Content.Server.Botany.Systems;
using Content.Server.Botany.Events;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Content.Server.Botany.Systems;
using Content.Shared.Interaction;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for managing special plant traits and mutations.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantTraitsComponent : Component
{
    [DataField]
    public List<PlantTrait> Traits = [];
}
