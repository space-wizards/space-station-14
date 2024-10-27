using System.Linq;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared._EinsteinEngine.Language;
using Content.Shared._EinsteinEngine.Language.Components;
using Content.Shared._EinsteinEngine.Language.Events;
using Content.Shared._EinsteinEngine.Language.Systems;
using Content.Shared.PowerCell;
using Content.Shared._EinsteinEngine.Language.Components.Translators;

namespace Content.Server._EinsteinEngine.Language;

// This does not support holding multiple translators at once.
// That shouldn't be an issue for now, but it needs to be fixed later.
public sealed class TranslatorSystem : SharedTranslatorSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicTranslatorComponent, DetermineEntityLanguagesEvent>(OnDetermineLanguages);
        SubscribeLocalEvent<HoldsTranslatorComponent, DetermineEntityLanguagesEvent>(OnDetermineLanguages);
        SubscribeLocalEvent<ImplantedTranslatorComponent, DetermineEntityLanguagesEvent>(OnDetermineLanguages);

        SubscribeLocalEvent<HandheldTranslatorComponent, ActivateInWorldEvent>(OnTranslatorToggle);
        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);

        SubscribeLocalEvent<HandheldTranslatorComponent, InteractHandEvent>(OnTranslatorInteract);
        SubscribeLocalEvent<HandheldTranslatorComponent, DroppedEvent>(OnTranslatorDropped);
    }

    private void OnDetermineLanguages(EntityUid uid, IntrinsicTranslatorComponent component, DetermineEntityLanguagesEvent ev)
    {
        if (!component.Enabled || !TryComp<LanguageSpeakerComponent>(uid, out var speaker))
            return;

        if (!_powerCell.HasActivatableCharge(uid))
            return;

        // The idea here is as follows:
        // Required languages are languages that are required to operate the translator.
        // The translator has a limited number of languages it can translate to and translate from.
        // If the wielder understands the language of the translator, they will be able to understand translations provided by it
        // If the wielder also speaks that language, they will be able to use it to translate their own speech by "speaking" in that language
        var addSpoken = CheckLanguagesMatch(component.RequiredLanguages, speaker.SpokenLanguages, component.RequiresAllLanguages);
        var addUnderstood = CheckLanguagesMatch(component.RequiredLanguages, speaker.UnderstoodLanguages, component.RequiresAllLanguages);

        if (addSpoken)
            foreach (var language in component.SpokenLanguages)
                ev.SpokenLanguages.Add(language);

        if (addUnderstood)
            foreach (var language in component.UnderstoodLanguages)
                ev.UnderstoodLanguages.Add(language);
    }

    private void OnTranslatorInteract(EntityUid translator, HandheldTranslatorComponent component, InteractHandEvent args)
    {
        var holder = args.User;
        if (!EntityManager.HasComponent<LanguageSpeakerComponent>(holder))
            return;

        var intrinsic = EnsureComp<HoldsTranslatorComponent>(holder);
        UpdateBoundIntrinsicComp(component, intrinsic, component.Enabled);

        _language.UpdateEntityLanguages(holder);
    }

    private void OnTranslatorDropped(EntityUid translator, HandheldTranslatorComponent component, DroppedEvent args)
    {
        var holder = args.User;
        if (!EntityManager.TryGetComponent<HoldsTranslatorComponent>(holder, out var intrinsic))
            return;

        if (intrinsic.Issuer == component)
        {
            intrinsic.Enabled = false;
            RemCompDeferred(holder, intrinsic);
        }

        _language.UpdateEntityLanguages(holder);
    }

    private void OnTranslatorToggle(EntityUid translator, HandheldTranslatorComponent translatorComp, ActivateInWorldEvent args)
    {
        if (!translatorComp.ToggleOnInteract)
            return;

        // This will show a popup if false
        var hasPower = _powerCell.HasDrawCharge(translator);

        if (Transform(args.Target).ParentUid is { Valid: true } holder
            && TryComp<LanguageSpeakerComponent>(holder, out var languageComp))
        {
            // This translator is held by a language speaker and thus has an intrinsic counterpart bound to it.
            // Make sure it's up-to-date.
            var intrinsic = EnsureComp<HoldsTranslatorComponent>(holder);
            var isEnabled = !translatorComp.Enabled;
            if (intrinsic.Issuer != translatorComp)
            {
                // The intrinsic comp wasn't owned by this handheld translator, so this wasn't the active translator.
                // Thus, the intrinsic comp needs to be turned on regardless of its previous state.
                intrinsic.Issuer = translatorComp;
                isEnabled = true;
            }
            isEnabled &= hasPower;

            UpdateBoundIntrinsicComp(translatorComp, intrinsic, isEnabled);
            translatorComp.Enabled = isEnabled;
            _powerCell.SetDrawEnabled(translator, isEnabled);

            // The first new spoken language added by this translator, or null
            var firstNewLanguage = translatorComp.SpokenLanguages.FirstOrDefault(it => !languageComp.SpokenLanguages.Contains(it));

            _language.UpdateEntityLanguages(holder, languageComp);

            // Update the current language of the entity if necessary
            if (isEnabled && translatorComp.SetLanguageOnInteract && firstNewLanguage is {})
                _language.SetLanguage(holder, firstNewLanguage, languageComp);
        }
        else
        {
            // This is a standalone translator (e.g. lying on the ground), toggle its state.
            translatorComp.Enabled = !translatorComp.Enabled && hasPower;
            _powerCell.SetDrawEnabled(translator, !translatorComp.Enabled && hasPower);
        }

        OnAppearanceChange(translator, translatorComp);

        if (hasPower)
        {
            var message = Loc.GetString(
                translatorComp.Enabled
                    ? "translator-component-turnon"
                    : "translator-component-shutoff",
                ("translator", translatorComp.Owner));
            _popup.PopupEntity(message, translatorComp.Owner, args.User);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid translator, HandheldTranslatorComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;
        _powerCell.SetDrawEnabled(translator, false);
        OnAppearanceChange(translator, component);

        if (Transform(translator).ParentUid is { Valid: true } holder
            && TryComp<LanguageSpeakerComponent>(holder, out var languageComp))
        {
            if (!EntityManager.TryGetComponent<HoldsTranslatorComponent>(holder, out var intrinsic))
                return;

            if (intrinsic.Issuer == component)
            {
                intrinsic.Enabled = false;
                RemComp(holder, intrinsic);
            }

            _language.UpdateEntityLanguages(holder, languageComp);
        }
    }

    /// <summary>
    ///   Copies the state from the handheld to the intrinsic component
    /// </summary>
    private void UpdateBoundIntrinsicComp(HandheldTranslatorComponent comp, HoldsTranslatorComponent intrinsic, bool isEnabled)
    {
        if (isEnabled)
        {
            intrinsic.SpokenLanguages = [..comp.SpokenLanguages];
            intrinsic.UnderstoodLanguages = [..comp.UnderstoodLanguages];
            intrinsic.RequiredLanguages = [..comp.RequiredLanguages];
        }
        else
        {
            intrinsic.SpokenLanguages.Clear();
            intrinsic.UnderstoodLanguages.Clear();
            intrinsic.RequiredLanguages.Clear();
        }

        intrinsic.Enabled = isEnabled;
        intrinsic.Issuer = comp;
    }

    /// <summary>
    ///     Checks whether any OR all required languages are provided. Used for utility purposes.
    /// </summary>
    public static bool CheckLanguagesMatch(ICollection<string> required, ICollection<string> provided, bool requireAll)
    {
        if (required.Count == 0)
            return true;

        return requireAll
            ? required.All(provided.Contains)
            : required.Any(provided.Contains);
    }
}
