using Content.Shared.Advertise;
using Robust.Shared.Prototypes;

namespace Content.Server.Advertise.Components;

/// <summary>
/// Causes the entity to speak using the Chat system when its ActivatableUI is closed, optionally
/// requiring that a Flag be set as well.
/// </summary>
[RegisterComponent, Access(typeof(SpeakOnUIClosedSystem))]
public sealed partial class SpeakOnUIClosedComponent : Component
{
    /// <summary>
    /// The identifier for the message pack prototype containing messages to be spoken by this entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MessagePackPrototype> Pack { get; private set; }

    /// <summary>
    /// Is this component active? If false, no messages will be spoken.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Should messages be spoken only if the <see cref="Flag"/> is set (true), or every time the UI is closed (false)?
    /// </summary>
    [DataField]
    public bool RequireFlag = true;

    /// <summary>
    /// State variable only used if <see cref="RequireFlag"/> is true. Set with <see cref="SpeakOnUIClosedSystem.TrySetFlag"/>.
    /// </summary>
    [DataField]
    public bool Flag;
}
