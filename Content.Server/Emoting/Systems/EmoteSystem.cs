using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Components;
using Content.Server.Speech.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Emoting;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Emoting.Systems;

public sealed class EmoteSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmotingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EmotingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmotingComponent, OpenEmotesActionEvent>(OnEmotingAction);

        SubscribeNetworkEvent<SelectEmoteEvent>(OnSelectEmote);
    }

    private void OnMapInit(EntityUid uid, EmotingComponent component, MapInitEvent args)
    {
        if (_proto.TryIndex(component.ActionId, out InstantActionPrototype? proto))
        {
            component.Action = new InstantAction(proto);
            _actions.AddAction(uid, component.Action, null);
        }
    }

    private void OnShutdown(EntityUid uid, EmotingComponent component, ComponentShutdown args)
    {
        if (component.Action != null)
        {
            _actions.RemoveAction(uid, component.Action);
        }
    }

    private void OnEmotingAction(EntityUid uid, EmotingComponent component, OpenEmotesActionEvent args)
    {
        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestEmoteMenuEvent(uid);

            foreach (var prototype in _proto.EnumeratePrototypes<EmotePrototype>())
            {
                // NOTE: Maybe need make some value in configuration.
                // If TRUE, we can put in menu next emotes, like: Meows, Honk, Heezes and something.
                // Or we can put only those emotes, what we can trigger with chat
                if (prototype.ChatTriggers.Count <= 0)
                    continue;

                switch (prototype.Category)
                {
                    case EmoteCategory.General:
                        ev.Prototypes.Add(prototype.ID);
                        break;
                    case EmoteCategory.Hands:
                        if (EntityManager.TryGetComponent<BodyEmotesComponent>(uid, out var _))
                            ev.Prototypes.Add(prototype.ID);
                        break;
                    case EmoteCategory.Vocal:
                        if (EntityManager.TryGetComponent<VocalComponent>(uid, out var _))
                            ev.Prototypes.Add(prototype.ID);
                        break;
                }
            }

            RaiseNetworkEvent(ev, actorComponent.PlayerSession);
        }

        args.Handled = true;
    }

    private void OnSelectEmote(SelectEmoteEvent msg)
    {
        _chat.TryEmoteWithChat(msg.Target, msg.PrototypeId);
    }
}
