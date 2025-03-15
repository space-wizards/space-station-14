using Content.Server.Abilities;

namespace Content.Server.Revenant.Components;

[RegisterComponent, Access(typeof(AbilitySystem))]
public sealed partial class OverloadLightsActionComponent : Component
{
    /// <summary>
    ///     The radius around the user that this ability affects.
    /// </summary>
    [DataField]
    public float OverloadRadius = 5f;

    /// <summary>
    ///     How close to the light the entity has to be in order to be zapped.
    /// </summary>
    [DataField]
    public float OverloadZapRadius = 2f;
}
