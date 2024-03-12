using Content.Server.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {traitId}!");
                return;
            }

            if (traitPrototype.Whitelist != null && !traitPrototype.Whitelist.IsValid(args.Mob))
                continue;

            if (traitPrototype.Blacklist != null && traitPrototype.Blacklist.IsValid(args.Mob))
                continue;

            // Add all components required by the prototype
            foreach (var entry in traitPrototype.Components.Values)
            {
                if (HasComp(args.Mob, entry.Component.GetType()))
                    continue;

                var comp = (Component) _serializationManager.CreateCopy(entry.Component, notNullableOverride: true);
                comp.Owner = args.Mob;
                EntityManager.AddComponent(args.Mob, comp);
            }

            // Add item required by the trait
            if (traitPrototype.TraitGear != null)
            {
                if (!TryComp(args.Mob, out HandsComponent? handsComponent))
                    continue;

                var coords = Transform(args.Mob).Coordinates;
                var inhandEntity = EntityManager.SpawnEntity(traitPrototype.TraitGear, coords);
                _sharedHandsSystem.TryPickup(args.Mob, inhandEntity, checkActionBlocker: false,
                    handsComp: handsComponent);
            }
        }
    }
}
