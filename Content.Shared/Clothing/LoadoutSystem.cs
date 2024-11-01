using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Clothing;

/// <summary>
/// Assigns a loadout to an entity based on the RoleLoadout prototype
/// </summary>
public sealed class LoadoutSystem : EntitySystem
{
    // Shared so we can predict it for placement manager.

    [Dependency] private readonly ActorSystem _actors = default!;
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Wait until the character has all their organs before we give them their loadout
        SubscribeLocalEvent<LoadoutComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedBodySystem)]);
    }

    public static string GetJobPrototype(string? loadout)
    {
        if (string.IsNullOrEmpty(loadout))
            return string.Empty;

        return "Job" + loadout;
    }

    public EntProtoId? GetFirstOrNull(LoadoutPrototype loadout)
    {
        EntProtoId? proto = null;

        if (_protoMan.TryIndex(loadout.StartingGear, out var gear))
        {
            proto = GetFirstOrNull(gear);
        }

        proto ??= GetFirstOrNull((IEquipmentLoadout)loadout);
        return proto;
    }

    /// <summary>
    /// Tries to get the first entity prototype for operations such as sprite drawing.
    /// </summary>
    public EntProtoId? GetFirstOrNull(IEquipmentLoadout? gear)
    {
        if (gear == null)
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

    public string GetName(LoadoutPrototype loadout)
    {
        if (_protoMan.TryIndex(loadout.StartingGear, out var gear))
        {
            return GetName(gear);
        }

        return GetName((IEquipmentLoadout) loadout);
    }

    /// <summary>
    /// Tries to get the name of a loadout.
    /// </summary>
    public string GetName(IEquipmentLoadout? gear)
    {
        if (gear == null)
            return string.Empty;

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

        return Loc.GetString($"unknown");
    }

    private void OnMapInit(EntityUid uid, LoadoutComponent component, MapInitEvent args)
    {
        Equip(uid, component.StartingGear, component.RoleLoadout);
    }

    public void Equip(EntityUid uid, List<ProtoId<StartingGearPrototype>>? startingGear,
        List<ProtoId<RoleLoadoutPrototype>>? loadoutGroups)
    {
        // First, randomly pick a startingGear profile from those specified, and equip it.
        if (startingGear != null && startingGear.Count > 0)
            _station.EquipStartingGear(uid, _random.Pick(startingGear));

        if (loadoutGroups == null)
        {
            GearEquipped(uid);
            return;
        }

        // Then, randomly pick a RoleLoadout profile from those specified, and process/equip all LoadoutGroups from it.
        // For non-roundstart mobs there is no SelectedLoadout data, so minValue must be set in each LoadoutGroup to force selection.
        var id = _random.Pick(loadoutGroups);
        var proto = _protoMan.Index(id);
        var loadout = new RoleLoadout(id);
        loadout.SetDefault(GetProfile(uid), _actors.GetSession(uid), _protoMan, true);
        _station.EquipRoleLoadout(uid, loadout, proto);

        GearEquipped(uid);
    }

    public void GearEquipped(EntityUid uid)
    {
        var ev = new StartingGearEquippedEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }

    public HumanoidCharacterProfile GetProfile(EntityUid? uid)
    {
        if (TryComp(uid, out HumanoidAppearanceComponent? appearance))
        {
            return HumanoidCharacterProfile.DefaultWithSpecies(appearance.Species);
        }

        return HumanoidCharacterProfile.Random();
    }
}
