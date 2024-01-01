using Content.Server.GameTicking.Rules;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Revolutionary.Components;

/// <summary>
/// Given to heads at round start for Revs. Used for tracking if heads died or not.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class ExiledComponent : Component
{
    /// <summary>
    /// If a person exceeds their exiled time they will be mark as Exiled.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Exiled = false;

    /// <summary>
    /// Marked on people that are currently in space and are actively losing exile time.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ConsideredForExile = false;

    /// <summary>
    /// The amount of time in seconds a person is allowed off station.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AllowedExileTime = TimeSpan.FromSeconds(120);

    /// <summary>
    /// When the next exile check should occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextExileCheck = TimeSpan.Zero;
}
