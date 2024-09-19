using Robust.Shared.Timing;

namespace Content.Client.TrueBlindness;

[RegisterComponent]
public sealed partial class TrueBlindnessGhostComponent : Component
{
    /// <summary>
    ///     The entity this ghost comes from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public NetEntity? From;

    /// <summary>
    ///     If the entity this came from was anchored when it was seen.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool WasAnchored;

    /// <summary>
    ///     How long this ghost will stay visible, if unanchored. Includes fadeout time.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan VisibleTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     How long this ghost takes to fade out.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan FadeoutTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    ///     When this ghost was created.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CreationTime = TimeSpan.Zero;

    /// <summary>
    ///     When this ghost will become eligible for deletion.
    ///     Set to the current time plus the entity it came from's BufferTime.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DeletionEligible = TimeSpan.Zero;
}
