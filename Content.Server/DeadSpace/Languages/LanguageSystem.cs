// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Random;
using Content.Shared.DeadSpace.Languages.Prototypes;
using Content.Shared.DeadSpace.Languages.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.Player;
using Content.Shared.DeadSpace.Languages;
using Robust.Server.Player;
using Content.Shared.Chat;
using System.Linq;

namespace Content.Server.DeadSpace.Languages;

public sealed class LanguageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public static readonly ProtoId<LanguagePrototype> DefaultLanguageId = "GeneralLanguage";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnComponentMapInit);
        SubscribeLocalEvent<LanguageComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LanguageComponent, SelectLanguageActionEvent>(OnSelect);

        SubscribeNetworkEvent<SelectLanguageEvent>(OnSelectLanguage);
    }

    private void OnComponentMapInit(EntityUid uid, LanguageComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.SelectLanguageActionEntity, component.SelectLanguageAction);
    }

    private void OnShutdown(EntityUid uid, LanguageComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SelectLanguageActionEntity);
    }

    private void OnSelect(EntityUid uid, LanguageComponent component, SelectLanguageActionEvent args)
    {
        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestLanguageMenuEvent(uid.Id, component.KnownLanguages, component.CantSpeakLanguages);
            RaiseNetworkEvent(ev, actorComponent.PlayerSession);
        }

        args.Handled = true;
    }

    private void OnSelectLanguage(SelectLanguageEvent msg)
    {
        if (EntityManager.TryGetComponent<LanguageComponent>(new EntityUid(msg.Target), out var language))
            language.SelectedLanguage = msg.PrototypeId;
    }

    public string ReplaceWordsWithLexicon(string message, ProtoId<LanguagePrototype> languageId)
    {
        if (!_prototypeManager.TryIndex(languageId, out var languageProto))
            return message;

        var lexiconWords = languageProto.Lexicon;

        if (lexiconWords == null || lexiconWords.Count == 0)
            return message;

        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(words[i]))
            {
                var randIndex = _random.Next(lexiconWords.Count);
                words[i] = lexiconWords[randIndex];
            }
        }
        return string.Join(' ', words);
    }

    public HashSet<ProtoId<LanguagePrototype>>? GetKnownLanguages(EntityUid entity)
    {
        if (!TryComp<LanguageComponent>(entity, out var component))
            return null;

        return component.KnownLanguages;
    }

    public bool KnowsLanguage(EntityUid receiver, ProtoId<LanguagePrototype> senderLanguageId)
    {
        var languages = GetKnownLanguages(receiver);

        if (languages == null) // если нет язков, значит знает всё
            return true;

        return languages.Contains(senderLanguageId);
    }

    public bool NeedGenerateTTS(EntityUid sourceUid, ProtoId<LanguagePrototype> prototypeId, bool isWhisper)
    {
        if (!_prototypeManager.TryIndex(prototypeId, out var languageProto))
            return false;

        if (!languageProto.GenerateTTSForLexicon)
            return false;

        float range = isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;

        var ents = _lookup.GetEntitiesInRange<ActorComponent>(_transform.GetMapCoordinates(sourceUid, Transform(sourceUid)), range).ToList();

        var hasListener = ents.Any(ent =>
            ent.Comp.PlayerSession is { AttachedEntity: not null }
            && !KnowsLanguage(ent.Owner, prototypeId));

        return hasListener;
    }

    public bool NeedGenerateDirectTTS(EntityUid uid, ProtoId<LanguagePrototype> prototypeId)
    {
        if (!_prototypeManager.TryIndex(prototypeId, out var languageProto))
            return false;

        if (!languageProto.GenerateTTSForLexicon)
            return false;

        if (KnowsLanguage(uid, prototypeId))
            return false;

        return true;
    }

    public bool NeedGenerateGlobalTTS(ProtoId<LanguagePrototype> prototypeId, out List<ICommonSession> understandings)
    {
        understandings = GetUnderstanding(prototypeId);

        if (!_prototypeManager.TryIndex(prototypeId, out var languageProto))
            return false;

        if (!languageProto.GenerateTTSForLexicon)
            return false;

        if (understandings.Count <= 0)
            return false;

        return true;
    }

    public bool NeedGenerateRadioTTS(ProtoId<LanguagePrototype> prototypeId, EntityUid[] recivers, out List<EntityUid> understandings, out List<EntityUid> notUnderstandings)
    {
        understandings = new List<EntityUid>();
        notUnderstandings = new List<EntityUid>();
        bool result = false;

        foreach (var uid in recivers)
        {
            if (!KnowsLanguage(uid, prototypeId))
            {
                notUnderstandings.Add(uid);
                result = true;
            }
            else
            {
                understandings.Add(uid);
            }
        }

        return result;
    }

    public List<ICommonSession> GetUnderstanding(ProtoId<LanguagePrototype> languageId)
    {
        var understanding = new List<ICommonSession>();

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity == null)
                continue;

            if (!HasComp<LanguageComponent>(session.AttachedEntity) || KnowsLanguage(session.AttachedEntity.Value, languageId)) // если нет язков, значит знает всё
                understanding.Add(session);
        }

        return understanding;
    }
}
