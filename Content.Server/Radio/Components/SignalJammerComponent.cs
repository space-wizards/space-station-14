namespace Content.Server.Radio.Components;

/// <summary>
/// A stationary signal jammer that must be anchored to work.
/// Jams radio communications within a large radius when powered.
/// </summary>
[RegisterComponent]
public sealed partial class SignalJammerComponent : Component
{
    /// <summary>
    /// The range at which this jammer will block radio communications.
    /// Much greater than the handheld radio jammer.
    /// </summary>
    [DataField]
    public float Range = 22f;
}
