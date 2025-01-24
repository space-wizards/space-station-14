using Content.Server.Abilities;

namespace Content.Server.Revenant.Components;

[RegisterComponent, Access(typeof(AbilitySystem))]
public sealed partial class DefileActionComponent : Component
{
    /// <summary>
    ///     The radius around the user that this ability affects
    /// </summary>
    [DataField]
    public float DefileRadius = 3.5f;

    /// <summary>
    ///     The amount of tiles that are uprooted by the ability
    /// </summary>
    [DataField]
    public int DefileTilePryAmount = 15;

    /// <summary>
    ///     The chance that an individual entity will have any of the effects
    ///     happen to it.
    /// </summary>
    [DataField]
    public float DefileEffectChance = 0.5f;
}
