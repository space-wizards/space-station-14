using Content.Server.GameTicking;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

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
                Logger.Warning($"No trait found with ID {traitId}!");
                return;
            }

            if (traitPrototype.Whitelist != null && !traitPrototype.Whitelist.IsValid(args.Mob))
                continue;

            if (traitPrototype.Blacklist != null && traitPrototype.Blacklist.IsValid(args.Mob))
                continue;

            // Add all components required by the prototype
            foreach (var entry in traitPrototype.Components.Values)
            {
                var comp = (Component) _serializationManager.Copy(entry.Component);
                comp.Owner = args.Mob;
                EntityManager.AddComponent(args.Mob, comp);
            }
        }
    }
}
