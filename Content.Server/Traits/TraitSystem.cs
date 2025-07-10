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
        if (args.Mind.OwnedEntity == null || args.Mind.UserId == null || args.MindRole.AntagPrototype == null || !_prototypeManager.TryIndex(args.MindRole.AntagPrototype, out var antag) || !antag.RevertTraits)
            return;

        // TODO: When we have a proper way to track character profiles to characters, it should be used here instead.
        var pref = (HumanoidCharacterProfile) _preferences.GetPreferences(args.Mind.UserId.Value).SelectedCharacter;

        foreach (var traitId in pref.TraitPreferences)
        {
            if (!pref.AntagDisableTraitPreferences.Contains(traitId))
                continue;

            if (!_prototypeManager.TryIndex(traitId, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {traitId}!");
                return;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, args.Mind.OwnedEntity.Value) ||
                _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, args.Mind.OwnedEntity.Value))
                continue;

            EntityManager.RemoveComponents(args.Mind.OwnedEntity.Value, traitPrototype.Components);
        }
    }
}
