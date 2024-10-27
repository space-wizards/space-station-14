using Content.Server._EinsteinEngine.Language.Events;
using Content.Server.Mind;
using Content.Shared._EinsteinEngine.Language.Components;
using Content.Shared._EinsteinEngine.Language.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server._EinsteinEngine.Language;

public sealed partial class LanguageSystem
{
    [Dependency] private readonly MindSystem _mind = default!;


    public void InitializeNet()
    {
        SubscribeNetworkEvent<LanguagesSetMessage>(OnClientSetLanguage);
        SubscribeNetworkEvent<RequestLanguagesMessage>((_, session) => SendLanguageStateToClient(session.SenderSession));

        SubscribeLocalEvent<LanguageSpeakerComponent, LanguagesUpdateEvent>((uid, comp, _) => SendLanguageStateToClient(uid, comp));

        // Refresh the client's state when its mind hops to a different entity
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>((uid, _, _) => SendLanguageStateToClient(uid));
        SubscribeLocalEvent<MindComponent, MindGotRemovedEvent>((_, _, args) =>
        {
            if (args.Mind.Comp.Session != null)
                SendLanguageStateToClient(args.Mind.Comp.Session);
        });
    }


    private void OnClientSetLanguage(LanguagesSetMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        var language = GetLanguagePrototype(message.CurrentLanguage);
        if (language == null || !CanSpeak(uid, language.ID))
            return;

        SetLanguage(uid, language.ID);
    }

    private void SendLanguageStateToClient(EntityUid uid, LanguageSpeakerComponent? comp = null)
    {
        // Try to find a mind inside the entity and notify its session
        if (!_mind.TryGetMind(uid, out _, out var mindComp) || mindComp.Session == null)
            return;

        SendLanguageStateToClient(uid, mindComp.Session, comp);
    }

    private void SendLanguageStateToClient(ICommonSession session, LanguageSpeakerComponent? comp = null)
    {
        // Try to find an entity associated with the session and resolve the languages from it
        if (session.AttachedEntity is not { Valid: true } entity)
            return;

        SendLanguageStateToClient(entity, session, comp);
    }

    // TODO this is really stupid and can be avoided if we just make everything shared...
    private void SendLanguageStateToClient(EntityUid uid, ICommonSession session, LanguageSpeakerComponent? component = null)
    {
        var isUniversal = HasComp<UniversalLanguageSpeakerComponent>(uid);
        if (!isUniversal)
            Resolve(uid, ref component, logMissing: false);

        // I really don't want to call 3 getter methods here, so we'll just have this slightly hardcoded solution
        var message = isUniversal || component == null
            ? new LanguagesUpdatedMessage(UniversalPrototype, [UniversalPrototype], [UniversalPrototype])
            : new LanguagesUpdatedMessage(component.CurrentLanguage, component.SpokenLanguages, component.UnderstoodLanguages);

        RaiseNetworkEvent(message, session);
    }
}
