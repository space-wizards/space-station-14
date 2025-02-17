using Content.Client._EinsteinEngines.Supermatter.Systems;
using Content.Shared._EinsteinEngines.Supermatter.Components;

namespace Content.Client._EinsteinEngines.Supermatter.Components;

[RegisterComponent]
[Access(typeof(SupermatterVisualizerSystem))]
public sealed partial class SupermatterVisualsComponent : Component
{
    [DataField("crystal", required: true)]
    public Dictionary<SupermatterCrystalState, PrototypeLayerData> CrystalVisuals = default!;
}
