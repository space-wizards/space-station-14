using System.Linq;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Components.Translators;
using Content.Shared._Starlight.Language.Events;
using Content.Shared._Starlight.Language.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Language;

public sealed class TranslatorSystem : SharedTranslatorSystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicTranslatorComponent, DetermineEntityLanguagesEvent>(OnDetermineLanguages);
        SubscribeLocalEvent<HoldsTranslatorComponent, DetermineEntityLanguagesEvent>(OnProxyDetermineLanguages);

        SubscribeLocalEvent<HandheldTranslatorComponent, EntGotInsertedIntoContainerMessage>(OnTranslatorInserted);
        SubscribeLocalEvent<HandheldTranslatorComponent, EntParentChangedMessage>(OnTranslatorParentChanged);
        SubscribeLocalEvent<HandheldTranslatorComponent, ActivateInWorldEvent>(OnTranslatorToggle);
        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<HandheldTranslatorComponent, ItemToggledEvent>(OnItemToggled);
    }

    private void OnDetermineLanguages(EntityUid uid, IntrinsicTranslatorComponent component, DetermineEntityLanguagesEvent ev)
    {
        if (!component.Enabled
            || component.LifeStage >= ComponentLifeStage.Removing
            || !TryComp<LanguageKnowledgeComponent>(uid, out var knowledge)
            || !_powerCell.HasActivatableCharge(uid))
            return;

        CopyLanguages(component, ev, knowledge);
    }

    private void OnProxyDetermineLanguages(EntityUid uid, HoldsTranslatorComponent component, DetermineEntityLanguagesEvent ev)
    {
        if (!TryComp<LanguageKnowledgeComponent>(uid, out var knowledge))
            return;

        foreach (var (translator, translatorComp) in component.Translators.ToArray())
        {
            if (!translatorComp.Enabled || !_powerCell.HasActivatableCharge(uid))
                continue;

            if (!_containers.TryGetContainingContainer(translator, out var container) || container.Owner != uid)
            {
                component.Translators.RemoveWhere(it => it.Owner == translator);
                continue;
            }

            CopyLanguages(translatorComp, ev, knowledge);
        }
    }

    private void OnTranslatorInserted(EntityUid translator, HandheldTranslatorComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.Owner is not {Valid: true} holder || !HasComp<LanguageSpeakerComponent>(holder))
            return;

        var intrinsic = EnsureComp<HoldsTranslatorComponent>(holder);
        intrinsic.Translators.Add((translator, component));

        _language.UpdateEntityLanguages(holder);
    }

    private void OnTranslatorParentChanged(EntityUid translator, HandheldTranslatorComponent component, EntParentChangedMessage args)
    {
        if (!HasComp<HoldsTranslatorComponent>(args.OldParent))
            return;

        // Update the translator on the next tick - this is necessary because there's a good chance the removal from a container.
        // Was caused by the player moving the translator within their inventory rather than removing it.
        // If that is not the case, then OnProxyDetermineLanguages will remove this translator from HoldsTranslatorComponent.Translators.
        Timer.Spawn(0, () =>
        {
            if (Exists(args.OldParent) && HasComp<LanguageSpeakerComponent>(args.OldParent))
                _language.UpdateEntityLanguages(args.OldParent.Value);
        });
    }

    private void OnTranslatorToggle(EntityUid translator, HandheldTranslatorComponent translatorComp, ActivateInWorldEvent args)
    {
        if (!translatorComp.ToggleOnInteract)
            return;

        // This will show a popup if false
        var hasPower = _powerCell.HasDrawCharge(translator);
        var isEnabled = !translatorComp.Enabled && hasPower;

        translatorComp.Enabled = isEnabled;
        _powerCell.SetDrawEnabled(translator, isEnabled);

        if (_containers.TryGetContainingContainer(translator, out var holderCont)
            && holderCont.Owner is var holder
            && TryComp<LanguageSpeakerComponent>(holder, out var languageComp))
        {
            // The first new spoken language added by this translator, or null
            var firstNewLanguage = translatorComp.SpokenLanguages.FirstOrDefault(it => !languageComp.SpokenLanguages.Contains(it));
            _language.UpdateEntityLanguages(holder);

            // Update the current language of the entity if necessary
            if (isEnabled && translatorComp.SetLanguageOnInteract && firstNewLanguage is {})
                _language.SetLanguage((holder, languageComp), firstNewLanguage);
        }

        OnAppearanceChange(translator, translatorComp);

        if (hasPower)
        {
            var loc = isEnabled ? "translator-component-turnon" : "translator-component-shutoff";
            var message = Loc.GetString(loc, ("translator", translator));
            _popup.PopupEntity(message, translator, args.User);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid translator, HandheldTranslatorComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;
        _powerCell.SetDrawEnabled(translator, false);
        OnAppearanceChange(translator, component);

        if (_containers.TryGetContainingContainer(translator, out var holderCont) && HasComp<LanguageSpeakerComponent>(holderCont.Owner))
            _language.UpdateEntityLanguages(holderCont.Owner);
    }

    private void OnPowerCellChanged(EntityUid translator, HandheldTranslatorComponent component, PowerCellChangedEvent args)
    {
        component.Enabled = !args.Ejected;
        _powerCell.SetDrawEnabled(translator, !args.Ejected);
        OnAppearanceChange(translator, component);

        if (_containers.TryGetContainingContainer((translator, null, null), out var holderCont) && HasComp<LanguageSpeakerComponent>(holderCont.Owner))
            _language.UpdateEntityLanguages(holderCont.Owner);
    }

    private void OnItemToggled(EntityUid translator, HandheldTranslatorComponent component, ItemToggledEvent args)
    {
        component.Enabled = args.Activated;
        _powerCell.SetDrawEnabled(translator, args.Activated);
        OnAppearanceChange(translator, component);

        if (_containers.TryGetContainingContainer((translator, null, null), out var holderCont) && HasComp<LanguageSpeakerComponent>(holderCont.Owner))
            _language.UpdateEntityLanguages(holderCont.Owner);
    }

    private void CopyLanguages(BaseTranslatorComponent from, DetermineEntityLanguagesEvent to, LanguageKnowledgeComponent knowledge)
    {
        var addSpoken = CheckLanguagesMatch(from.RequiredLanguages, knowledge.SpokenLanguages, from.RequiresAllLanguages);
        var addUnderstood = CheckLanguagesMatch(from.RequiredLanguages, knowledge.UnderstoodLanguages, from.RequiresAllLanguages);

        if (addSpoken)
            foreach (var language in from.SpokenLanguages)
                to.SpokenLanguages.Add(language);

        if (addUnderstood)
            foreach (var language in from.UnderstoodLanguages)
                to.UnderstoodLanguages.Add(language);
    }

    /// <summary>
    ///     Checks whether any OR all required languages are provided. Used for utility purposes.
    /// </summary>
    public static bool CheckLanguagesMatch(ICollection<ProtoId<LanguagePrototype>> required, ICollection<ProtoId<LanguagePrototype>> provided, bool requireAll)
    {
        if (required.Count == 0)
            return true;

        return requireAll
            ? required.All(provided.Contains)
            : required.Any(provided.Contains);
    }
}