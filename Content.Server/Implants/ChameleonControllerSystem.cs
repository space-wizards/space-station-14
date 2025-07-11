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

        ChangeChameleonClothingToOutfit(ent.Comp.ImplantedEntity.Value, args.SelectedChameleonOutfit);
    }

    /// <summary>
    ///     Switches all the chameleon clothing that the implant user is wearing to look like the selected job.
    /// </summary>
    private void ChangeChameleonClothingToOutfit(EntityUid user, ProtoId<ChameleonOutfitPrototype> outfit)
    {
        var outfitPrototype = _proto.Index(outfit);

        _proto.TryIndex(outfitPrototype.Job, out var jobPrototype);
        _proto.TryIndex(outfitPrototype.StartingGear, out var startingGearPrototype);

        GetJobEquipmentInformation(jobPrototype, user, out var customRoleLoadout, out var defaultRoleLoadout, out var jobStartingGearPrototype);

        var ev = new ChameleonControllerOutfitSelectedEvent(
            outfitPrototype,
            customRoleLoadout,
            defaultRoleLoadout,
            jobStartingGearPrototype,
            startingGearPrototype
            );

        RaiseLocalEvent(user, ref ev);
    }

    // This gets as much information from the job as it can.
    // E.g. the players profile, the default equipment for that job etc...
    private void GetJobEquipmentInformation(
        JobPrototype? jobPrototype,
        EntityUid? user,
        out RoleLoadout? customRoleLoadout,
        out RoleLoadout? defaultRoleLoadout,
        out StartingGearPrototype? jobStartingGearPrototype)
    {
        customRoleLoadout = null;
        defaultRoleLoadout = null;
        jobStartingGearPrototype = null;

        if (jobPrototype == null)
            return;

        _proto.TryIndex(jobPrototype.StartingGear, out jobStartingGearPrototype);

        if (!TryComp<ActorComponent>(user, out var actorComponent))
            return;

        var userId = actorComponent.PlayerSession.UserId;
        var prefs = _preferences.GetPreferences(userId);

        if (prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            return;

        var jobProtoId = LoadoutSystem.GetJobPrototype(jobPrototype.ID);

        profile.Loadouts.TryGetValue(jobProtoId, out customRoleLoadout);

        if (!_proto.HasIndex<RoleLoadoutPrototype>(jobProtoId))
            return;

        defaultRoleLoadout = new RoleLoadout(jobProtoId);
        defaultRoleLoadout.SetDefault(profile, null, _proto); // only sets the default if the player has no loadout
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

    /// <summary>
    /// Get the gear for the given slot. The priority is:
    /// <br/>1.) Custom loadout from the player for the slot.
    /// <br/>2.) Chameleon outfit slot equipment.
    /// <br/>3.) Chameleon outfit starting gear equipment.
    /// <br/>4.) Default job equipment.
    /// <br/>5.) Staring equipment for that job.
    /// </summary>
    /// <returns>The entity (as a protoid) if there is gear for that slot, null if there isn't.</returns>
    public string? GetGearForSlot(ChameleonOutfitPrototype? chameleonOutfitPrototype, RoleLoadout? customRoleLoadout, RoleLoadout? defaultRoleLoadout, StartingGearPrototype? jobStartingGearPrototype, StartingGearPrototype? startingGearPrototype, string slotName)
    {
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
