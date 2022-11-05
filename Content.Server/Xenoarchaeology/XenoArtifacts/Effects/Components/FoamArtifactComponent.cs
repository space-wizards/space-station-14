using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

[RegisterComponent]
public sealed class FoamArtifactComponent : Component
{
    [DataField("reagents", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public List<string> Reagents = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public string? SelectedReagent;

    [DataField("duration")]
    public float Duration = 10;

    [DataField("reagentAmount")]
    public float ReagentAmount = 100;

    [DataField("minFoamAmount")]
    public int MinFoamAmount = 2;

    [DataField("maxFoamAmount")]
    public int MaxFoamAmount = 6;

    [DataField("spreadDuration")]
    public float SpreadDuration = 1;
}
