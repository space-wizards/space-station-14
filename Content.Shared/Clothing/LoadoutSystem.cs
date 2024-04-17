using System.Linq;
using Content.Shared.Clothing.Components;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Clothing;

/// <summary>
/// Assigns a loadout to an entity based on the startingGear prototype
/// </summary>
public sealed class LoadoutSystem : EntitySystem
{
    // Shared so we can predict it for placement manager.

    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadoutComponent, MapInitEvent>(OnMapInit);
    }

    public static string GetJobPrototype(string? loadout)
    {
        if (string.IsNullOrEmpty(loadout))
            return string.Empty;

        return "Job" + loadout;
    }

    /// <summary>
    /// Tries to get the first entity prototype for operations such as sprite drawing.
    /// </summary>
    public EntProtoId? GetFirstOrNull(LoadoutPrototype loadout)
    {
        if (!_protoMan.TryIndex(loadout.Equipment, out var gear))
            return null;

        var count = gear.Equipment.Count + gear.Inhand.Count + gear.Storage.Values.Sum(x => x.Count);

        if (count == 1)
        {
            if (gear.Equipment.Count == 1 && _protoMan.TryIndex<EntityPrototype>(gear.Equipment.Values.First(), out var proto))
            {
                return proto.ID;
            }

            if (gear.Inhand.Count == 1 && _protoMan.TryIndex<EntityPrototype>(gear.Inhand[0], out proto))
            {
                return proto.ID;
            }

            // Storage moment
            foreach (var ents in gear.Storage.Values)
            {
                foreach (var ent in ents)
                {
                    return ent;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to get the name of a loadout.
    /// </summary>
    public string GetName(LoadoutPrototype loadout)
    {
        if (!_protoMan.TryIndex(loadout.Equipment, out var gear))
            return Loc.GetString("loadout-unknown");

        var count = gear.Equipment.Count + gear.Storage.Values.Sum(o => o.Count) + gear.Inhand.Count;

        if (count == 1)
        {
            if (gear.Equipment.Count == 1 && _protoMan.TryIndex<EntityPrototype>(gear.Equipment.Values.First(), out var proto))
            {
                return proto.Name;
            }

            if (gear.Inhand.Count == 1 && _protoMan.TryIndex<EntityPrototype>(gear.Inhand[0], out proto))
            {
                return proto.Name;
            }

            foreach (var values in gear.Storage.Values)
            {
                if (values.Count != 1)
                    continue;

                if (_protoMan.TryIndex<EntityPrototype>(values[0], out proto))
                {
                    return proto.Name;
                }

                break;
            }
        }

        return Loc.GetString($"loadout-{loadout.ID}");
    }

    private void OnMapInit(EntityUid uid, LoadoutComponent component, MapInitEvent args)
    {
        if (component.Prototypes == null)
            return;

        var proto = _protoMan.Index<StartingGearPrototype>(_random.Pick(component.Prototypes));
        _station.EquipStartingGear(uid, proto);
    }
}
