using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components;

[RegisterComponent]
public sealed partial class SignalTimerComponent : Component
{
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite)]
    public double Delay = 5;

    /// <summary>
    ///     This shows the Label: text box in the UI.
    /// </summary>
    [DataField("canEditLabel"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanEditLabel = true;

    /// <summary>
    ///     The label, used for TextScreen visuals currently.
    /// </summary>
    [DataField("label"), ViewVariables(VVAccess.ReadWrite)]
    public string Label = string.Empty;

    /// <summary>
    ///     The port that gets signaled when the timer triggers.
    /// </summary>
    [DataField("triggerPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string TriggerPort = "Timer";

    /// <summary>
    ///     The port that gets signaled when the timer starts.
    /// </summary>
    [DataField("startPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string StartPort = "Start";

    /// <summary>
    ///     If not null, this timer will play this sound when done.
    /// </summary>
    [DataField("doneSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? DoneSound;
}
