using Content.Server.Actions;
using Content.Server.IdentityManagement;
using Content.Shared.Changeling.Transform;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Prototypes;
using Content.Shared.Speech.Components;
using Content.Shared.Wagging;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.Changeling.Transform;

public sealed class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly ActionsSystem _actionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformWindupDoAfterEvent>(OnSuccessfulTransform);
    }
    private void OnSuccessfulTransform(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformWindupDoAfterEvent args)
    {
        args.Handled = true;
        if (args.Cancelled)
            return;
        if(!TryComp<HumanoidAppearanceComponent>(uid, out var currentAppearance))
            return;
        if(!TryComp<VocalComponent>(uid, out var currentVocals))
            return;
        if(!TryComp<DnaComponent>(uid, out var currentDna))
            return;
        if(!TryComp<GrammarComponent>(uid, out var currentGrammar))
            return;
        if (!TryComp<IdentityComponent>(uid, out var currentIdentity))
            return;
        var identityContainedUid = currentIdentity!.IdentityEntitySlot.ContainedEntities[0];
        if(!TryComp<GrammarComponent>(identityContainedUid, out var identityGrammar))
            return;


        var lastConsumedAppearance  = component.ChangelingIdentities?.LastConsumedIdentityComponent?.IdentityAppearance;
        var lastConsumedVocals = component.ChangelingIdentities?.LastConsumedIdentityComponent?.IdentityVocals;
        var lastConsumedDna = component.ChangelingIdentities?.LastConsumedIdentityComponent?.IdentityDna;
        var lastConsumedname = component.ChangelingIdentities?.LastConsumedIdentityComponent?.IdentityName;
        var lastConsumedDescription = component.ChangelingIdentities?.LastConsumedIdentityComponent?.IdentityDescription;
        var grammar = new Entity<GrammarComponent>(uid, currentGrammar);
        var identityGrammarEntity = new Entity<GrammarComponent>(identityContainedUid, identityGrammar!);

        if (lastConsumedAppearance != null || lastConsumedVocals != null || lastConsumedDna != null)
        {
            currentAppearance.Species = lastConsumedAppearance!.Species;
            if (TryComp<WaggingComponent>(uid, out var waggingComp))
            {
                _actionSystem.RemoveAction(uid, waggingComp.ActionEntity);
                RemComp<WaggingComponent>(uid);
            }
            if (EntityPrototypeHelpers.HasComponent<WaggingComponent>(component.ChangelingIdentities?.LastConsumedIdentityComponent?.IdentityEntityPrototype!)
                && !HasComp<WaggingComponent>(uid))
            {
                EnsureComp<WaggingComponent>(uid, out _);
            }
            currentAppearance.Age = lastConsumedAppearance!.Age;
            currentAppearance.Gender = lastConsumedAppearance.Gender;
            currentAppearance.EyeColor = lastConsumedAppearance.EyeColor;
            currentAppearance.Gender = lastConsumedAppearance.Gender;
            currentAppearance.Sex = lastConsumedAppearance.Sex;
            currentAppearance.BaseLayers = lastConsumedAppearance.BaseLayers;
            currentAppearance.SkinColor = lastConsumedAppearance.SkinColor;
            currentAppearance.HiddenLayers = lastConsumedAppearance.HiddenLayers;
            currentAppearance.MarkingSet = lastConsumedAppearance.MarkingSet;
            currentAppearance.CachedHairColor = lastConsumedAppearance.CachedHairColor;
            currentAppearance.CustomBaseLayers = lastConsumedAppearance.CustomBaseLayers;
            currentAppearance.CachedFacialHairColor = lastConsumedAppearance.CachedFacialHairColor;
            currentDna.DNA = lastConsumedDna!.DNA;

            currentVocals.EmoteSounds = lastConsumedVocals!.EmoteSounds;
            currentVocals.Sounds = lastConsumedVocals!.Sounds;
            currentVocals.ScreamAction = lastConsumedVocals!.ScreamAction;
            currentVocals.ScreamId = lastConsumedVocals!.ScreamId;
            currentVocals.Wilhelm = lastConsumedVocals!.Wilhelm;
            currentVocals.WilhelmProbability = lastConsumedVocals!.WilhelmProbability;

            _metaSystem.SetEntityName(uid, lastConsumedname!, raiseEvents: false);
            _metaSystem.SetEntityDescription(uid, lastConsumedDescription!);

            //How to stop quantum gender, a bug where the Examine pronoun will mispredict 100% of the time. Need to SPECIFICALLY
            // also modify the Identities grammar component, before Queuing the identityUpdate
            _grammarSystem.SetGender(grammar, lastConsumedAppearance.Gender);
            _grammarSystem.SetGender(identityGrammarEntity, lastConsumedAppearance.Gender);
            _identitySystem.QueueIdentityUpdate(uid);
        }
        _entityManager.Dirty(uid, currentAppearance);
    }
}

