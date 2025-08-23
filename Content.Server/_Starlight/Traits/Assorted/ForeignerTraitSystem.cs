using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server._Starlight.Language;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Components.Translators;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Traits.Assorted;


public sealed partial class ForeignerTraitSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LanguageSystem _languages = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ForeignerTraitComponent, ComponentInit>(OnSpawn); // TraitSystem adds it after PlayerSpawnCompleteEvent so it's fine.
    }

    private void OnSpawn(Entity<ForeignerTraitComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.CantUnderstand && !entity.Comp.CantSpeak)
            Log.Warning($"Allowing entity {entity.Owner} to speak a language but not understand it leads to undefined behavior.");

        if (!TryComp<LanguageKnowledgeComponent>(entity, out var knowledge))
        {
            Log.Warning($"Entity {entity.Owner} does not have a LanguageKnowledge but has a ForeignerTrait!");
            return;
        }

        var alternateLanguage = knowledge.SpokenLanguages.Find(it => it != entity.Comp.BaseLanguage);
        if (alternateLanguage == default)
        {
            Log.Warning($"Entity {entity.Owner} does not have an alternative language to choose from (must have at least one non-GC for ForeignerTrait)!");
            return;
        }

        if (TryGiveTranslator(entity.Owner, entity.Comp.BaseTranslator, entity.Comp.BaseLanguage, alternateLanguage, out var translator))
        {
            _languages.RemoveLanguage(entity.Owner, entity.Comp.BaseLanguage, entity.Comp.CantSpeak, entity.Comp.CantUnderstand);
        }
    }

    /// <summary>
    ///     Tries to create and give the entity a translator that translates speech between the two specified languages.
    /// </summary>
    public bool TryGiveTranslator(
        EntityUid uid,
        string baseTranslatorPrototype,
        ProtoId<LanguagePrototype> translatorLanguage,
        ProtoId<LanguagePrototype> entityLanguage,
        out EntityUid result)
    {
        result = EntityUid.Invalid;
        if (translatorLanguage == entityLanguage)
            return false;

        var translator = _entMan.SpawnNextToOrDrop(baseTranslatorPrototype, uid);
        result = translator;

        if (!TryComp<HandheldTranslatorComponent>(translator, out var handheld))
        {
            handheld = AddComp<HandheldTranslatorComponent>(translator);
            handheld.ToggleOnInteract = true;
            handheld.SetLanguageOnInteract = true;
        }

        // Allows to speak the specified language and requires entities language.
        handheld.SpokenLanguages = [translatorLanguage];
        handheld.UnderstoodLanguages = [translatorLanguage];
        handheld.RequiredLanguages = [entityLanguage];

        // Try to put it in entities hand
        if (_hands.TryPickupAnyHand(uid, translator, false, false, false))
            return true;

        // Try to find a valid clothing slot on the entity and equip the translator there
        if (TryComp<ClothingComponent>(translator, out var clothing)
            && clothing.Slots != SlotFlags.NONE
            && _inventory.TryGetSlots(uid, out var slots)
            && slots.Any(it => _inventory.TryEquip(uid, translator, it.Name, true, false)))
            return true;

        // Try to put the translator into entities bag, if it has one
        if (_inventory.TryGetSlotEntity(uid, "back", out var bag)
            && TryComp<StorageComponent>(bag, out var storage)
            && _storage.Insert(bag.Value, translator, out _, null, storage, false, false))
            return true;

        // If all of the above has failed, just drop it at the same location as the entity
        // This should ideally never happen, but who knows.
        Transform(translator).Coordinates = Transform(uid).Coordinates;

        return true;
    }
}