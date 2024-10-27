using System.Linq;
using Content.Server._EinsteinEngine.Language.Events;
using Content.Shared._EinsteinEngine.Language;
using Content.Shared._EinsteinEngine.Language.Components;
using Content.Shared._EinsteinEngine.Language.Systems;
using UniversalLanguageSpeakerComponent = Content.Shared._EinsteinEngine.Language.Components.UniversalLanguageSpeakerComponent;

namespace Content.Server._EinsteinEngine.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeNet();

        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentInit>(OnInitLanguageSpeaker);
    }


    #region public api

    public bool CanUnderstand(EntityUid listener, string language, LanguageSpeakerComponent? component = null)
    {
        if (language == UniversalPrototype || HasComp<UniversalLanguageSpeakerComponent>(listener))
            return true;

        if (!Resolve(listener, ref component, logMissing: false))
            return false;

        return component.UnderstoodLanguages.Contains(language);
    }

    public bool CanSpeak(EntityUid speaker, string language, LanguageSpeakerComponent? component = null)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(speaker))
            return true;

        if (!Resolve(speaker, ref component, logMissing: false))
            return false;

        return component.SpokenLanguages.Contains(language);
    }

    /// <summary>
    ///     Returns the current language of the given entity, assumes Universal if it's not a language speaker.
    /// </summary>
    public LanguagePrototype GetLanguage(EntityUid speaker, LanguageSpeakerComponent? component = null)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(speaker) || !Resolve(speaker, ref component, logMissing: false))
            return Universal; // Serves both as a fallback and uhhh something (TODO: fix this comment)

        if (string.IsNullOrEmpty(component.CurrentLanguage) || !_prototype.TryIndex<LanguagePrototype>(component.CurrentLanguage, out var proto))
            return Universal;

        return proto;
    }

    /// <summary>
    ///     Returns the list of languages this entity can speak.
    /// </summary>
    /// <remarks>Typically, checking <see cref="LanguageSpeakerComponent.SpokenLanguages"/> is sufficient.</remarks>
    public List<string> GetSpokenLanguages(EntityUid uid)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(uid))
            return [UniversalPrototype];

        if (TryComp<LanguageSpeakerComponent>(uid, out var component))
            return component.SpokenLanguages;

        return [];
    }

    /// <summary>
    ///     Returns the list of languages this entity can understand.
    /// </summary>
    /// <remarks>Typically, checking <see cref="LanguageSpeakerComponent.UnderstoodLanguages"/> is sufficient.</remarks>
    public List<string> GetUnderstoodLanguages(EntityUid uid)
    {
        if (HasComp<UniversalLanguageSpeakerComponent>(uid))
            return [UniversalPrototype]; // This one is tricky because... well, they understand all of them, not just one.

        if (TryComp<LanguageSpeakerComponent>(uid, out var component))
            return component.UnderstoodLanguages;

        return [];
    }

    public void SetLanguage(EntityUid speaker, string language, LanguageSpeakerComponent? component = null)
    {
        if (!CanSpeak(speaker, language) || (HasComp<UniversalLanguageSpeakerComponent>(speaker) && language != UniversalPrototype))
            return;

        if (!Resolve(speaker, ref component) || component.CurrentLanguage == language)
            return;

        component.CurrentLanguage = language;
        RaiseLocalEvent(speaker, new LanguagesUpdateEvent(), true);
    }

    /// <summary>
    ///     Adds a new language to the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public void AddLanguage(
        EntityUid uid,
        string language,
        bool addSpoken = true,
        bool addUnderstood = true,
        LanguageKnowledgeComponent? knowledge = null,
        LanguageSpeakerComponent? speaker = null)
    {
        if (knowledge == null)
            knowledge = EnsureComp<LanguageKnowledgeComponent>(uid);

        if (addSpoken && !knowledge.SpokenLanguages.Contains(language))
            knowledge.SpokenLanguages.Add(language);

        if (addUnderstood && !knowledge.UnderstoodLanguages.Contains(language))
            knowledge.UnderstoodLanguages.Add(language);

        UpdateEntityLanguages(uid, speaker);
    }

    /// <summary>
    ///     Removes a language from the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public void RemoveLanguage(
        EntityUid uid,
        string language,
        bool removeSpoken = true,
        bool removeUnderstood = true,
        LanguageKnowledgeComponent? knowledge = null,
        LanguageSpeakerComponent? speaker = null)
    {
        if (knowledge == null)
            knowledge = EnsureComp<LanguageKnowledgeComponent>(uid);

        if (removeSpoken)
            knowledge.SpokenLanguages.Remove(language);

        if (removeUnderstood)
            knowledge.UnderstoodLanguages.Remove(language);

        UpdateEntityLanguages(uid, speaker);
    }

    /// <summary>
    ///   Ensures the given entity has a valid language as its current language.
    ///   If not, sets it to the first entry of its SpokenLanguages list, or universal if it's empty.
    /// </summary>
    /// <returns>True if the current language was modified, false otherwise.</returns>
    public bool EnsureValidLanguage(EntityUid entity, LanguageSpeakerComponent? comp = null)
    {
        if (!Resolve(entity, ref comp))
            return false;

        if (!comp.SpokenLanguages.Contains(comp.CurrentLanguage))
        {
            comp.CurrentLanguage = comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);
            RaiseLocalEvent(entity, new LanguagesUpdateEvent());
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Immediately refreshes the cached lists of spoken and understood languages for the given entity.
    /// </summary>
    public void UpdateEntityLanguages(EntityUid entity, LanguageSpeakerComponent? languages = null)
    {
        if (!Resolve(entity, ref languages))
            return;

        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryComp<LanguageKnowledgeComponent>(entity, out var knowledge))
        {
            foreach (var spoken in knowledge.SpokenLanguages)
                ev.SpokenLanguages.Add(spoken);

            foreach (var understood in knowledge.UnderstoodLanguages)
                ev.UnderstoodLanguages.Add(understood);
        }

        RaiseLocalEvent(entity, ref ev);

        languages.SpokenLanguages.Clear();
        languages.UnderstoodLanguages.Clear();

        languages.SpokenLanguages.AddRange(ev.SpokenLanguages);
        languages.UnderstoodLanguages.AddRange(ev.UnderstoodLanguages);

        if (!EnsureValidLanguage(entity))
            RaiseLocalEvent(entity, new LanguagesUpdateEvent());
    }

    #endregion

    #region event handling

    private void OnInitLanguageSpeaker(EntityUid uid, LanguageSpeakerComponent component, ComponentInit args)
    {
        if (string.IsNullOrEmpty(component.CurrentLanguage))
            component.CurrentLanguage = component.SpokenLanguages.FirstOrDefault(UniversalPrototype);

        UpdateEntityLanguages(uid, component);
    }

    #endregion
}
