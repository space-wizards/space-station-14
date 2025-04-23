using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Starlight.Medical.Items.Components;

[RegisterComponent]
public sealed partial class PatchUserComponent : Component
{
    [DataField]
    public List<ReagentQuantity> ReagentsToInsert = new();
    
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
    
    [DataField]
    public float ReagentInjectAmount = 0.5f;
}