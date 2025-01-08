namespace Content.Server.Event.Components;

/// <summary>
/// Flickers all the lights within a certain radius.
/// </summary>
[RegisterComponent]
public sealed partial class EventReactorComponent : Component
{
    /// <summary>
    /// Lights within this radius will be flickered on activation
    /// </summary>
    [DataField("radius")]
    public float Radius = 9999;

    /// <summary>
    /// The chance that the light will flicker
    /// </summary>
    [DataField("flickerChance")]
    public float FlickerChance = 0.85f;

    [DataField("firstWarning")]
    public bool FirstWarning = false;

    [DataField("secondWarning")]
    public bool SecondWarning = false;

    [DataField("thirdWarning")]
    public bool ThirdWarning = false;

    [DataField("meltdownWarning")]
    public bool MeltdownWarning = false;

     [ViewVariables(VVAccess.ReadWrite)]
     [DataField(required: true)]
     public LocId Title = "ship-comms-title";

     /// <summary>
     /// Announcement color
    /// </summary>
    [ViewVariables]
    [DataField]
    public Color Color = Color.Red;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/alert.ogg");

}
