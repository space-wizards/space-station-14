using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevenantOverloadLightsActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float StunDuration = 2f;

    [DataField, AutoNetworkedField]
    public float CorporealDuration = 8f;

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OverloadRadius = 5f;

    /// <summary>
    /// How close to the light the entity has to be in order to be zapped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OverloadZapRadius = 2f;
}
