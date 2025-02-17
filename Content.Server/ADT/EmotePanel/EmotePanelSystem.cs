using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.EmotePanel;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Server.Emoting.Components;
using Content.Shared.Sirena.Animations;
using Content.Shared.Speech.Components;
using Content.Shared.Whitelist;
using Content.Shared.Speech;

namespace Content.Server.ADT.EmotePanel;

/// <summary>
/// EmotePanelSystem process actions on "ActionOpenEmotes" and RadialUi.
/// <see cref="Content.Shared.ADT.EmotePanel.EmotePanelComponent"/>
/// </summary>
public sealed class EmotePanelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly EntityManager _entManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmotePanelComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EmotePanelComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmotePanelComponent, OpenEmotesActionEvent>(OnEmotingAction);

        SubscribeNetworkEvent<SelectEmoteEvent>(OnSelectEmote);
    }

    private void OnMapInit(EntityUid uid, EmotePanelComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.OpenEmotesActionEntity, component.OpenEmotesAction);
    }

    private void OnShutdown(EntityUid uid, EmotePanelComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.OpenEmotesActionEntity);
    }

    /// <summary>
    /// Gathers emotes-prototypes and sends to client, which trigger OpenEmotesActionEvent.
    /// </summary>
    /// <param name="uid">source of action</param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnEmotingAction(EntityUid uid, EmotePanelComponent component, OpenEmotesActionEvent args)
    {
        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestEmoteMenuEvent(uid.Id);

            foreach (var emote in _proto.EnumeratePrototypes<EmotePrototype>())
            {
                if (emote.Category == EmoteCategory.Invalid ||
                    emote.ChatTriggers.Count == 0 ||
                    !(_whitelistSystem.IsWhitelistPassOrNull(emote.Whitelist, uid)) ||
                    _whitelistSystem.IsBlacklistPass(emote.Blacklist, uid))
                    continue;

                if (!emote.Available &&
                    _entManager.TryGetComponent<SpeechComponent>(uid, out var speech) &&
                    !speech.AllowedEmotes.Contains(emote.ID))
                    continue;

                if (emote.ID == "Scream" || emote.ID == "EmoteStopTail" || emote.ID == "EmoteStartTail") // TODO: FIX
                    continue;

                switch (emote.Category)
                {
                    case EmoteCategory.General:
                        ev.Prototypes.Add(emote.ID);
                        break;
                    case EmoteCategory.Hands:
                        if (EntityManager.TryGetComponent<BodyEmotesComponent>(uid, out var _))
                            ev.Prototypes.Add(emote.ID);
                        break;
                    case EmoteCategory.Vocal:
                        if (EntityManager.TryGetComponent<VocalComponent>(uid, out var _))
                            ev.Prototypes.Add(emote.ID);
                        break;
                    case EmoteCategory.Animations:
                        if (EntityManager.TryGetComponent<EmoteAnimationComponent>(uid, out var _))
                            ev.Prototypes.Add(emote.ID);
                        break;
                }
            }
            ev.Prototypes.Sort();
            RaiseNetworkEvent(ev, actorComponent.PlayerSession);
        }

        args.Handled = true;
    }
    private void OnSelectEmote(SelectEmoteEvent msg)
    {
        _chat.TryEmoteWithChat(new EntityUid(msg.Target), msg.PrototypeId);
    }
}
