using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Tag;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.TraitRandomizer;

public sealed partial class TraitRandomizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitRandomizerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TraitRandomizerComponent> ent, ref MapInitEvent args)
    {
        if (!_mind.TryGetMind(ent, out _, out var mindComponent) || !_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
            return;

        var allTraits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().ToList();
        List<TraitPrototype> traits = [];

        // make a list of the traits we should be adding
        foreach (var trait in allTraits)
        {
            foreach (var category in ent.Comp.Categories)
            {
                if (trait.Category == _prototypeManager.Index(category))
                    traits.Add(trait);
            }
        }

        var curProfile = (HumanoidCharacterProfile)_prefs.GetPreferences(session.UserId).SelectedCharacter;

        var curTraits = curProfile.TraitPreferences.ToList();

        // remove currently applied traits from the list of traits we can roll from.
        foreach (var traitProto in curTraits)
        {
            var trait = _prototypeManager.Index(traitProto);
            traits.Remove(trait);
        }

        // how many traits are we gonna get?
        var traitsToRoll = _random.Next(ent.Comp.MinTraits, ent.Comp.MaxTraits + 1);
        List<TraitPrototype> finalTraits = [];

        // pick a trait, ensure we don't pick it again, and add it to the final traits list. do this that many times.
        // note: currently this ignores points limits, because I think it's funnier that way.
        for (var i = 0; i < traitsToRoll; i++)
        {
            var thisTrait = _random.Pick(traits);
            allTraits.Remove(thisTrait);
            finalTraits.Add(thisTrait);
        }

        foreach (var traitId in finalTraits)
        {
            if (_whitelistSystem.IsWhitelistFail(traitId.Whitelist, ent) ||
                _whitelistSystem.IsBlacklistPass(traitId.Blacklist, ent))
                continue;

            // Add all components required by the prototype to the body or specified organ
            if (traitId.Organ != null)
            {
                foreach (var organ in _bodySystem.GetBodyOrgans(ent))
                {
                    if (traitId.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
                    {
                        EntityManager.AddComponents(organ.Id, traitId.Components);
                    }
                }
            }
            else
            {
                EntityManager.AddComponents(ent, traitId.Components, false);
            }

            // Add item required by the trait
            if (traitId.TraitGear == null)
                continue;

            if (!TryComp(ent, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(ent).Coordinates;
            var inhandEntity = EntityManager.SpawnEntity(traitId.TraitGear, coords);
            _sharedHandsSystem.TryPickup(ent,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
}
