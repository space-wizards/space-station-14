using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.DeviceLinking.Events;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Handles the speech on activating an entity
/// </summary>
public sealed partial class SpeakOnActivateSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeakOnActivateComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SpeakOnActivateComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnUseInHand(Entity<SpeakOnActivateComponent> ent, ref UseInHandEvent args)
    {
        TrySpeak(ent);
    }

    private void OnSignalReceived(Entity<SpeakOnActivateComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.ActivatePort)
            return;

        TrySpeak(ent);
    }

    #region Helper functions
    public void TrySpeak(Entity<SpeakOnActivateComponent> ent)
    {
        // Yes it won't work without UseDelayComponent, but we don't want any kind of spam
        if (!TryComp(ent.Owner, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((ent.Owner, useDelay)))
            return;

        if (!_prototypeManager.TryIndex(ent.Comp.Pack, out var messagePack))
            return;

        var message = Loc.GetString(_random.Pick(messagePack.Values));
        _chat.TrySendInGameICMessage(ent.Owner, message, InGameICChatType.Speak, true);
        _useDelay.TryResetDelay((ent.Owner, useDelay));
    }
    #endregion
}
