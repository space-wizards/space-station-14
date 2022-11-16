using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
public sealed class TraumaInflictorComponent : Component
{
    [Access(typeof(InjurySystem), Other = AccessPermissions.Read)] [DataField("Trauma", required: true)]
    public TraumaSpecifier Trauma = new TraumaSpecifier();
}
