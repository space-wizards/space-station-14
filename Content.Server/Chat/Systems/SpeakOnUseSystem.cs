using Content.Server.Chat;
using Content.Shared.Dataset;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Handles the speech on activating an entity
/// </summary>
public sealed partial class SpeakOnUIClosedSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeakOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    public void OnUseInHand(EntityUid uid, SpeakOnUseComponent? component, UseInHandEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        // Yes it won't work without UseDelayComponent, but we don't want any kind of spam
        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!_prototypeManager.TryIndex(component.Pack, out var messagePack))
            return;

        var message = Loc.GetString(_random.Pick(messagePack.Values));
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, true);
        _useDelay.TryResetDelay((uid, useDelay));
    }
}
