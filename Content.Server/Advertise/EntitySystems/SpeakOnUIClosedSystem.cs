using Content.Server.Chat.Systems;
using Content.Shared.Advertise.Components;
using Content.Shared.Advertise.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Advertise.EntitySystems;

public sealed partial class SpeakOnUIClosedSystem : SharedSpeakOnUIClosedSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnUIClosedComponent, BoundUIClosedEvent>(OnBoundUIClosed);
    }
    private void OnBoundUIClosed(Entity<SpeakOnUIClosedComponent> entity, ref BoundUIClosedEvent args)
    {
        if (!TryComp(entity, out ActivatableUIComponent? activatable) || !args.UiKey.Equals(activatable.Key))
            return;

        if (entity.Comp.RequireFlag && !entity.Comp.Flag)
            return;

        TrySpeak((entity, entity.Comp));
    }

    public bool TrySpeak(Entity<SpeakOnUIClosedComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!entity.Comp.Enabled)
            return false;

        if (!_prototypeManager.TryIndex(entity.Comp.Pack, out var messagePack))
            return false;

        var message = Loc.GetString(_random.Pick(messagePack.Values), ("name", Name(entity)));
        _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, true);
        entity.Comp.Flag = false;
        return true;
    }
}
