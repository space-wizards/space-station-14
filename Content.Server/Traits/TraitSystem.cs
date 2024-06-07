using Content.Server.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        ApplyTraits(args.Mob, args.Profile.TraitPreferences);
    }

    public void ApplyTraits(EntityUid uid, IReadOnlySet<ProtoId<TraitPrototype>> traits)
    {
        // Construct a dict of all the categories that support limits
        var categoryLimits = new Dictionary<string, int>();
        foreach (var traitCategory in _prototypeManager.EnumeratePrototypes<TraitCategoryPrototype>())
        {
            if (traitCategory.MaxTraitPoints != null)
                categoryLimits.Add(traitCategory.ID, traitCategory.MaxTraitPoints.Value);
        }

        foreach (var traitId in traits)
        {
            if (!_prototypeManager.TryIndex(traitId, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {traitId}!");
                return;
            }

            if (traitPrototype.Category != null &&
                categoryLimits.TryGetValue(traitPrototype.Category, out var pointsRemaining))
            {
                if (traitPrototype.Cost > pointsRemaining)
                {
                    Log.Error($"Unable to apply trait {traitPrototype.ID}! Too many traits in category {traitPrototype.Category} selected.");
                    continue;
                }

                categoryLimits[traitPrototype.Category] = pointsRemaining - traitPrototype.Cost;
            }


            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, uid) ||
                _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, uid))
                continue;

            // Add all components required by the prototype
            EntityManager.AddComponents(uid, traitPrototype.Components, false);

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(uid, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(uid).Coordinates;
            var inhandEntity = EntityManager.SpawnEntity(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(uid,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }

    }
}
