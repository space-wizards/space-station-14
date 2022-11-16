using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(InjurySystem))]
public sealed class TraumaInflictorComponent : Component
{
    [DataField("Trauma", required: true)]
    public TraumaSpecifier Trauma = new TraumaSpecifier();
}
