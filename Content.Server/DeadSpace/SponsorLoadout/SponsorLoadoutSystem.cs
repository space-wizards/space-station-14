using System.Linq;
using Content.Shared.GameTicking;
using Content.Server.Hands.Systems;
using Content.Shared.DeadSpace.SponsorLoadout;
using Robust.Shared.Prototypes;
using Content.DeadSpace.Interfaces.Server;

namespace Content.Server.DeadSpace.SponsorLoadout;

public sealed class SponsorLoadoutSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    private IServerSponsorsManager? _sponsorsManager; // DS14-sponsors

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);

        IoCManager.Instance!.TryResolveType(out _sponsorsManager); // DS14-sponsors
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_sponsorsManager?.TryGetInfo(ev.Player.UserId, out var sponsor) == true)
        {
            foreach (var loadoutId in sponsor.AllowedMarkings)
            {
                // NOTE: Now is easy to not extract method because event give all info we need
                if (_prototypeManager.TryIndex<SponsorLoadoutPrototype>(loadoutId, out var loadout))
                {
                    var isSponsorOnly = loadout.SponsorOnly &&
                                        !sponsor.AllowedMarkings.Contains(loadoutId);
                    var isWhitelisted = ev.JobId != null &&
                                        loadout.WhitelistJobs != null &&
                                        !loadout.WhitelistJobs.Contains(ev.JobId);
                    var isBlacklisted = ev.JobId != null &&
                                        loadout.BlacklistJobs != null &&
                                        loadout.BlacklistJobs.Contains(ev.JobId);
                    var isSpeciesRestricted = loadout.SpeciesRestrictions != null &&
                                              loadout.SpeciesRestrictions.Contains(ev.Profile.Species);

                    if (isSponsorOnly || isWhitelisted || isBlacklisted || isSpeciesRestricted)
                        continue;

                    var entity = Spawn(loadout.EntityId, Transform(ev.Mob).Coordinates);

                    _handsSystem.TryPickup(ev.Mob, entity);
                }
            }
        }
    }
}
