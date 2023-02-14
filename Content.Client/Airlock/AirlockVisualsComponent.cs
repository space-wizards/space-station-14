namespace Content.Client.Airlock;

[RegisterComponent]
public sealed class AirlockVisualsComponent : Component
{
    [DataField("animationTime")]
    public float Delay = 0.8f;

    [DataField("denyAnimationTime")]
    public float DenyDelay = 0.3f;


    [DataField("emagAnimationTime")]
    public float DelayEmag = 1.5f;

    /// <summary>
    ///     Whether the maintenance panel is animated or stays static.
    ///     False for windoors.
    /// </summary>
    [DataField("animatedPanel")]
    public bool AnimatedPanel = true;

    /// <summary>
    /// Means the door is simply open / closed / opening / closing. No wires or access.
    /// </summary>
    [DataField("simpleVisuals")]
    public bool SimpleVisuals = false;

    /// <summary>
    ///     Whether the BaseUnlit layer should still be visible when the airlock
    ///     is opened.
    /// </summary>
    [DataField("openUnlitVisible")]
    public bool OpenUnlitVisible = false;

    /// <summary>
    ///     Whether the door should have an emergency access layer
    /// </summary>
    [DataField("emergencyAccessLayer")]
    public bool EmergencyAccessLayer = true;
}
