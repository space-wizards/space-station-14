using Content.Server.Clothing.Systems;
using Content.Server.Preferences.Managers;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using Content.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Implants;

public sealed class ChameleonControllerSystem : SharedChameleonControllerSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly ChameleonClothingSystem _chameleonClothingSystem = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, ChameleonControllerSelectedOutfitMessage>(OnSelected);

        SubscribeLocalEvent<ChameleonClothingComponent, InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent>>(ChameleonControllerOutfitItemSelected);
    }

    private void OnSelected(Entity<SubdermalImplantComponent> ent, ref ChameleonControllerSelectedOutfitMessage args)
    {
        if (!_delay.TryResetDelay(ent.Owner, true) || ent.Comp.ImplantedEntity == null || !HasComp<ChameleonControllerImplantComponent>(ent))
            return;

        ChangeChameleonClothingToJob(ent.Comp.ImplantedEntity.Value, args.SelectedChameleonOutfit);
    }

    /// <summary>
    ///     Switches all the chameleon clothing that the implant user is wearing to look like the selected job.
    /// </summary>
    private void ChangeChameleonClothingToJob(EntityUid user, ProtoId<ChameleonOutfitPrototype> outfit)
    {
        var outfitPrototype = _proto.Index(outfit);

        _proto.TryIndex(outfitPrototype.Job, out var jobPrototype);
        _proto.TryIndex(outfitPrototype.StartingGear, out var startingGearPrototype);

        RoleLoadout? customRoleLoadout = null;
        RoleLoadout? defaultRoleLoadout = null;
        StartingGearPrototype? jobStartingGearPrototype = null;

        if (jobPrototype != null)
        {
            _proto.TryIndex(jobPrototype.StartingGear, out jobStartingGearPrototype);

            if (!TryComp<ActorComponent>(user, out var actorComponent))
                goto slotIterator;

            var userId = actorComponent.PlayerSession.UserId;
            var prefs = _preferences.GetPreferences(userId);

            if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
                goto slotIterator;

            var jobProtoId = LoadoutSystem.GetJobPrototype(jobPrototype.ID);

            profile.Loadouts.TryGetValue(jobProtoId, out customRoleLoadout);

            if (!_proto.HasIndex<RoleLoadoutPrototype>(jobProtoId))
                goto slotIterator;

            defaultRoleLoadout = new RoleLoadout(jobProtoId);
            defaultRoleLoadout.SetDefault(profile, null, _proto); // only sets the default if the player has no loadout
        }

        slotIterator:

        var ev = new ChameleonControllerOutfitSelectedEvent(
            outfitPrototype,
            customRoleLoadout,
            defaultRoleLoadout,
            jobStartingGearPrototype,
            startingGearPrototype
            );

        RaiseLocalEvent(user, ref ev);
    }

    private void ChameleonControllerOutfitItemSelected(Entity<ChameleonClothingComponent> ent, ref InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent> args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot))
            return;

        _chameleonClothingSystem.SetSelectedPrototype(ent, GetGearForSlot(args, slot.Name), component: ent.Comp);
    }

    public string? GetGearForSlot(InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent> ev, string slotName)
    {
        return GetGearForSlot(ev.Args.ChameleonOutfit, ev.Args.CustomRoleLoadout, ev.Args.DefaultRoleLoadout, ev.Args.JobStartingGearPrototype, ev.Args.StartingGearPrototype, slotName);
    }

    public string? GetGearForSlot(ChameleonOutfitPrototype? chameleonOutfitPrototype, RoleLoadout? customRoleLoadout, RoleLoadout? defaultRoleLoadout, StartingGearPrototype? jobStartingGearPrototype, StartingGearPrototype? startingGearPrototype, string slotName)
    {
        // Priority is:
        // 1.) Custom loadout from the player for the slot.
        // 2.) Chameleon outfit slot equipment.
        // 3.) Chameleon outfit starting gear equipment.
        // 4.) Default job equipment.
        // 5.) Staring equipment for that job.

        var customLoadoutGear = _stationSpawningSystem.GetGearForSlot(customRoleLoadout, slotName);
        if (customLoadoutGear != null)
            return customLoadoutGear;

        if (chameleonOutfitPrototype != null && chameleonOutfitPrototype.Equipment.TryGetValue(slotName, out var forSlot))
            return forSlot;

        var startingGear = startingGearPrototype != null ? ((IEquipmentLoadout)startingGearPrototype).GetGear(slotName) : "";
        if (startingGear != "")
            return startingGear;

        var defaultLoadoutGear = _stationSpawningSystem.GetGearForSlot(defaultRoleLoadout, slotName);
        if (defaultLoadoutGear != null)
            return defaultLoadoutGear;

        var jobStartingGear = jobStartingGearPrototype != null ? ((IEquipmentLoadout)jobStartingGearPrototype).GetGear(slotName) : "";
        if (jobStartingGear != "")
            return jobStartingGear;

        return null;
    }
}
