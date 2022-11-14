using Content.Server.Radio.Components;
using Content.Shared.Interaction;
using Content.Shared.Radio;
using Content.Shared.Whitelist;

namespace Content.Server.SurveillanceCamera;

/// <summary>
///     Component that allows surveillance cameras to listen to the local
///     environment. All surveillance camera monitors have speakers for this.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(IListen))]
public sealed class SurveillanceCameraMicrophoneComponent : Component, IListen
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Components that the microphone checks for to avoid transmitting
    ///     messages from these entities over the surveillance camera.
    ///     Used to avoid things like feedback loops, or radio spam.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist BlacklistedComponents { get; } = new();

    // TODO: Once IListen is removed, **REMOVE THIS**

    private SurveillanceCameraMicrophoneSystem? _microphoneSystem;
    protected override void Initialize()
    {
        base.Initialize();

        _microphoneSystem = EntitySystem.Get<SurveillanceCameraMicrophoneSystem>();
    }

    public int ListenRange { get; } = 10;
    public bool CanListen(string message, EntityUid source, RadioChannelPrototype? channelPrototype)
    {
        return _microphoneSystem != null
            && _microphoneSystem.CanListen(Owner, source, this);
    }

    public void Listen(string message, EntityUid speaker, RadioChannelPrototype? channel)
    {
        _microphoneSystem?.RelayEntityMessage(Owner, speaker, message);
    }
}
