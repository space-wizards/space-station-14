using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

[RegisterComponent]
public sealed partial class SignalTimerComponent : Component
{
    [DataField]
    public double Delay = 5;

    /// <summary>
    ///     This shows the Label: text box in the UI.
    /// </summary>
    [DataField]
    public bool CanEditLabel = true;

    /// <summary>
    ///     The label, used for TextScreen visuals currently.
    /// </summary>
    [DataField]
    public string Label = string.Empty;

    /// <summary>
    ///     Default max width of a label (how many letters can this render?)
    /// </summary>
    [DataField]
    public int MaxLength = 5;

    /// <summary>
    ///     The port that gets signaled when the timer triggers.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> TriggerPort = "Timer";

    /// <summary>
    ///     The port that gets signaled when the timer starts.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> StartPort = "Start";

    [DataField]
    public ProtoId<SinkPortPrototype> Trigger = "Trigger";

    /// <summary>
    ///     If not null, this timer will play this sound when done.
    /// </summary>
    [DataField]
    public SoundSpecifier? DoneSound;

    /// <summary>
    ///     The maximum duration in seconds
    ///     When a larger number is in the input box, the display will start counting down from this one instead
    /// </summary>
    [DataField]
    public Double MaxDuration = 3599; // 59m 59s
}
