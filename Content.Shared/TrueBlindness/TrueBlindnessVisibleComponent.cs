namespace Content.Client.TrueBlindness;

[RegisterComponent]
public sealed partial class TrueBlindnessVisibleComponent : Component
{
    /// <summary>
    ///     How long the entity will stay visible, if unanchored. Includes fadeout time.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan VisibleTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     How long an entity takes to fade out.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan FadeoutTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    ///     How long this entity will have to wait before another ghost can be spawned from it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BufferTime = TimeSpan.FromMilliseconds(100);

    /// <summary>
    ///     The last time this entity spawned a ghost.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastGhost = TimeSpan.Zero;
}
