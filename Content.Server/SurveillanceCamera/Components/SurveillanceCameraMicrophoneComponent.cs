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
    public int ListenRange { get; }
    public bool CanListen(string message, EntityUid source, RadioChannelPrototype? channelPrototype)
    {
        return Enabled
               && !BlacklistedComponents.IsValid(source)
               && EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(Owner, source, range: ListenRange);
    }

    public void Listen(string message, EntityUid speaker, RadioChannelPrototype? channel)
    {
        EntitySystem.Get<SurveillanceCameraMicrophoneSystem>().RelayEntityMessage(Owner, speaker, message);
    }
}
