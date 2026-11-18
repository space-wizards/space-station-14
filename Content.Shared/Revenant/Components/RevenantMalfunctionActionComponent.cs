using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevenantMalfunctionActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float StunDuration = 2f;

    [DataField, AutoNetworkedField]
    public float CorporealDuration = 8f;

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MalfunctionRadius = 3.5f;

    /// <summary>
    /// Whitelist for entities that can be emagged by malfunction.
    /// Used to prevent ultra gamer things like ghost emagging chem or instantly launching the shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? MalfunctionWhitelist;

    /// <summary>
    /// Whitelist for entities that can never be emagged by malfunction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? MalfunctionBlacklist;
}
