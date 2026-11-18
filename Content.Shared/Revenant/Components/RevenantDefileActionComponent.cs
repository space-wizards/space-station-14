using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevenantDefileActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float StunDuration = 1f;

    [DataField, AutoNetworkedField]
    public float CorporealDuration = 4f;

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DefileRadius = 3.5f;

    /// <summary>
    /// The amount of tiles that are uprooted by the ability
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DefileTilePryAmount = 15;

    /// <summary>
    /// The chance that an individual entity will have any of the effects
    /// happen to it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DefileEffectChance = 0.5f;
}
