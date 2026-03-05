using Content.Server.Preferences.Managers;
using Content.Shared.Clothing.Systems;
using Content.Shared.Clothing;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Player;

namespace Content.Server.Clothing.Systems;

public sealed class OutfitSystem : SharedOutfitSystem
{
    [Dependency] private readonly IServerPreferencesManager _preferenceManager = default!;
    [Dependency] private readonly SharedStationSpawningSystem _spawningSystem = default!;

    public override bool SetOutfit(EntityUid target, string gear, Action<EntityUid, EntityUid>? onEquipped = null, bool unremovable = false, bool stripEmptySlots = true, bool respectEquippability = false)
    {
        var ret = base.SetOutfit(target, gear, onEquipped, unremovable, stripEmptySlots, respectEquippability);

        if (!ret)
            return false;

        HumanoidCharacterProfile? profile = null;
        ICommonSession? session = null;
        // Check if we are setting the outfit of a player to respect the preferences
        if (TryComp(target, out ActorComponent? actorComponent))
        {
            session = actorComponent.PlayerSession;
            var userId = actorComponent.PlayerSession.UserId;
            var prefs = _preferenceManager.GetPreferences(userId);
            profile = prefs.SelectedCharacter as HumanoidCharacterProfile;
        }

        // See if this starting gear is associated with a job
        var jobs = PrototypeManager.EnumeratePrototypes<JobPrototype>();
        foreach (var job in jobs)
        {
            if (job.StartingGear != gear)
                continue;

            var jobProtoId = LoadoutSystem.GetJobPrototype(job.ID);
            if (!PrototypeManager.TryIndex<RoleLoadoutPrototype>(jobProtoId, out var jobProto))
                break;

            // Don't require a player, so this works on Urists
            profile ??= TryComp<HumanoidProfileComponent>(target, out var comp)
                ? HumanoidCharacterProfile.DefaultWithSpecies(comp.Species, comp.Sex)
                : new HumanoidCharacterProfile();
            // Try to get the user's existing loadout for the role
            profile.Loadouts.TryGetValue(jobProtoId, out var roleLoadout);

            if (roleLoadout == null)
            {
                // If they don't have a loadout for the role, make a default one
                roleLoadout = new RoleLoadout(jobProtoId);
                roleLoadout.SetDefault(profile, session, PrototypeManager);
            }

            // Equip the target with the job loadout
            _spawningSystem.EquipRoleLoadout(target, roleLoadout, jobProto);
        }

        return true;
    }
}
