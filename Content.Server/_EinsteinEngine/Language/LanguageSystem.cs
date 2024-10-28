using System.Linq;
using Content.Shared._EinsteinEngine.Language;
using Content.Shared._EinsteinEngine.Language.Components;
using Content.Shared._EinsteinEngine.Language.Events;
using Content.Shared._EinsteinEngine.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._EinsteinEngine.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageKnowledgeComponent, MapInitEvent>(OnInitLanguageKnowledge);
        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnInitLanguageSpeaker);
        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentGetState>(OnGetLanguageState);
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, DetermineEntityLanguagesEvent>(OnDetermineUniversalLanguages);
        SubscribeNetworkEvent<LanguagesSetMessage>(OnClientSetLanguage);

        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, MapInitEvent>((uid, _, _) => UpdateEntityLanguages(uid));
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, ComponentRemove>((uid, _, _) => UpdateEntityLanguages(uid));
    }

    #region event handling
    private void OnInitLanguageKnowledge(Entity<LanguageKnowledgeComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<LanguageSpeakerComponent>(ent.Owner);
    }

    private void OnInitLanguageSpeaker(Entity<LanguageSpeakerComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.CurrentLanguage))
            ent.Comp.CurrentLanguage = ent.Comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);

        UpdateEntityLanguages(ent!);
    }

    private void OnGetLanguageState(Entity<LanguageSpeakerComponent> entity, ref ComponentGetState args)
    {
        args.State = new LanguageSpeakerComponent.State
        {
            CurrentLanguage = entity.Comp.CurrentLanguage,
            SpokenLanguages = entity.Comp.SpokenLanguages,
            UnderstoodLanguages = entity.Comp.UnderstoodLanguages
        };
    }

    private void OnDetermineUniversalLanguages(Entity<UniversalLanguageSpeakerComponent> entity, ref DetermineEntityLanguagesEvent ev)
    {
        // We only add it as a spoken language; CanUnderstand checks for ULSC itself.
        if (entity.Comp.Enabled)
            ev.SpokenLanguages.Add(UniversalPrototype);
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

    #endregion

    #region public api

    public bool CanUnderstand(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (language == UniversalPrototype || TryComp<UniversalLanguageSpeakerComponent>(ent, out var uni) && uni.Enabled)
            return true;

        return Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.UnderstoodLanguages.Contains(language);
    }

    public bool CanSpeak(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        return ent.Comp.SpokenLanguages.Contains(language);
    }

    /// <summary>
    ///     Returns the current language of the given entity, assumes Universal if it's not a language speaker.
    /// </summary>
    public LanguagePrototype GetLanguage(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || string.IsNullOrEmpty(ent.Comp.CurrentLanguage)
            || !_prototype.TryIndex<LanguagePrototype>(ent.Comp.CurrentLanguage, out var proto)
        )
            return Universal;

        return proto;
    }

    /// <summary>
    ///     Returns the list of languages this entity can speak.
    /// </summary>
    /// <remarks>This simply returns the value of <see cref="LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetSpokenLanguages(EntityUid uid)
    {
        return TryComp<LanguageSpeakerComponent>(uid, out var component) ? component.SpokenLanguages : [];
    }

    /// <summary>
    ///     Returns the list of languages this entity can understand.
    /// </summary
    /// <remarks>This simply returns the value of <see cref="LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetUnderstoodLanguages(EntityUid uid)
    {
        return TryComp<LanguageSpeakerComponent>(uid, out var component) ? component.UnderstoodLanguages : [];
    }

    public void SetLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!CanSpeak(ent, language)
            || !Resolve(ent, ref ent.Comp)
            || ent.Comp.CurrentLanguage == language)
            return;

        ent.Comp.CurrentLanguage = language;
        RaiseLocalEvent(ent, new LanguagesUpdateEvent(), true);
        Dirty(ent);
    }

    /// <summary>
    ///     Adds a new language to the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public void AddLanguage(
        EntityUid uid,
        ProtoId<LanguagePrototype> language,
        bool addSpoken = true,
        bool addUnderstood = true)
    {
        EnsureComp<LanguageKnowledgeComponent>(uid, out var knowledge);
        EnsureComp<LanguageSpeakerComponent>(uid, out var speaker);

        if (addSpoken && !knowledge.SpokenLanguages.Contains(language))
            knowledge.SpokenLanguages.Add(language);

        if (addUnderstood && !knowledge.UnderstoodLanguages.Contains(language))
            knowledge.UnderstoodLanguages.Add(language);

        UpdateEntityLanguages((uid, speaker));
    }

    /// <summary>
    ///     Removes a language from the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public void RemoveLanguage(
        Entity<LanguageKnowledgeComponent?> ent,
        ProtoId<LanguagePrototype> language,
        bool removeSpoken = true,
        bool removeUnderstood = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (removeSpoken)
            ent.Comp.SpokenLanguages.Remove(language);

        if (removeUnderstood)
            ent.Comp.UnderstoodLanguages.Remove(language);

        // We don't ensure that the entity has a speaker comp. If it doesn't... Well, woe be the caller of this method.
        UpdateEntityLanguages(ent.Owner);
    }

    /// <summary>
    ///   Ensures the given entity has a valid language as its current language.
    ///   If not, sets it to the first entry of its SpokenLanguages list, or universal if it's empty.
    /// </summary>
    /// <returns>True if the current language was modified, false otherwise.</returns>
    public bool EnsureValidLanguage(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.SpokenLanguages.Contains(ent.Comp.CurrentLanguage))
        {
            ent.Comp.CurrentLanguage = ent.Comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);
            RaiseLocalEvent(ent, new LanguagesUpdateEvent());
            Dirty(ent);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Immediately refreshes the cached lists of spoken and understood languages for the given entity.
    /// </summary>
    public void UpdateEntityLanguages(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryComp<LanguageKnowledgeComponent>(ent, out var knowledge))
        {
            foreach (var spoken in knowledge.SpokenLanguages)
                ev.SpokenLanguages.Add(spoken);

            foreach (var understood in knowledge.UnderstoodLanguages)
                ev.UnderstoodLanguages.Add(understood);
        }

        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpokenLanguages.Clear();
        ent.Comp.UnderstoodLanguages.Clear();

        ent.Comp.SpokenLanguages.AddRange(ev.SpokenLanguages);
        ent.Comp.UnderstoodLanguages.AddRange(ev.UnderstoodLanguages);

        // If EnsureValidLanguage returns true, it also raises a LanguagesUpdateEvent, so we try to avoid raising it twice in that case.
        if (!EnsureValidLanguage(ent))
            RaiseLocalEvent(ent, new LanguagesUpdateEvent());

        Dirty(ent);
    }

    #endregion
}
