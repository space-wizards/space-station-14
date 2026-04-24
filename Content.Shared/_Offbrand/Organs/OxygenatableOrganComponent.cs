using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OxygenatableOrganSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class OxygenatableOrganComponent : Component
{
    /// <summary>
    /// The maximum amount of oxygen this brain can store
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxOxygen;

    /// <summary>
    /// The current amount of stored brain oxygen
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Oxygen;
}

[ByRefEvent]
public readonly record struct OrganOxygenChangedEvent;
