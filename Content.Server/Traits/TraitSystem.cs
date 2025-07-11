using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAddedEvent);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !_prototypeManager.TryIndex<JobPrototype>(args.JobId ?? string.Empty, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        if (args.Profile.TraitPreferences.Count == 0)
            return;

        // We add the TraitsComponent here to remember all the traits that were applied
        var traitsComp = EnsureComp<TraitsComponent>(args.Mob);

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {traitId}!");
                return;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, args.Mob) ||
                _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, args.Mob))
                continue;

            // Add all components required by the prototype
            EntityManager.AddComponents(args.Mob, traitPrototype.Components, false);

            traitsComp.AppliedTraits.Add(new (traitId, args.Profile.AntagDisableTraitPreferences.Contains(traitId)));

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(args.Mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(args.Mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(args.Mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }

    // We optionally disable traits for antags that should have them disabled.
    private void OnRoleAddedEvent(RoleAddedEvent args)
    {
        if (args.MindEntity.Comp.OwnedEntity == null || args.MindRoleEntity.Comp.AntagPrototype == null)
            return;

        if (!TryComp<TraitsComponent>(args.MindEntity.Comp.OwnedEntity, out var traitsComp) ||
            !_prototypeManager.TryIndex(args.MindRoleEntity.Comp.AntagPrototype, out var antag) || !antag.RevertTraits)
            return;

        var traitSet = traitsComp.AppliedTraits;

        foreach (var trait in traitSet)
        {
            if (!trait.Revertable)
                continue;

            if (!_prototypeManager.TryIndex(trait.Trait, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {trait.Trait}!");
                return;
            }

            if (_whitelistSystem.CheckBoth(args.MindEntity.Comp.OwnedEntity.Value, traitPrototype.Blacklist, traitPrototype.Whitelist))
                continue;

            EntityManager.RemoveComponents(args.MindEntity.Comp.OwnedEntity.Value, traitPrototype.Components);
            traitsComp.AppliedTraits.Remove(trait);
        }
    }
}
