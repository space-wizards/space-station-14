using Content.Server.Abilities;
using Content.Shared.Whitelist;

namespace Content.Server.Revenant.Components;

[RegisterComponent, Access(typeof(AbilitySystem))]
public sealed partial class MalfunctionActionComponent : Component
{
    /// <summary>
    ///     The radius around the user that this ability affects.
    /// </summary>
    [DataField]
    public float MalfunctionRadius = 3.5f;

    /// <summary>
    ///     <see cref="EntityWhitelist"/> for entities that can be emagged by malfunction.
    ///     Used to prevent ultra gamer things like ghost emagging chem or instantly launching the shuttle.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionWhitelist;

    /// <summary>
    ///     <see cref="EntityWhitelist"/> for entities that can never be emagged by malfunction.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionBlacklist;
}
