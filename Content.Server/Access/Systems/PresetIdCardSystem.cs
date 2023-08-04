using Content.Server.Access.Components;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Systems
{
    public sealed class PresetIdCardSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IdCardSystem _cardSystem = default!;
        [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<PresetIdCardComponent, MapInitEvent>(OnMapInit);

            SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(PlayerJobsAssigned);
        }

        private void PlayerJobsAssigned(RulePlayerJobsAssignedEvent ev)
        {
            // Go over all ID cards and make sure they're correctly configured for extended access.

            foreach (var card in EntityQuery<PresetIdCardComponent>())
            {
                var station = _stationSystem.GetOwningStation(card.Owner);

                // If we're not on an extended access station, the ID is already configured correctly from MapInit.
                if (station == null || !Comp<StationJobsComponent>(station.Value).ExtendedAccess)
                    return;

                SetupIdAccess(card.Owner, card, true);
            }
        }

        private void OnMapInit(EntityUid uid, PresetIdCardComponent id, MapInitEvent args)
        {
            // If a preset ID card is spawned on a station at setup time,
            // the station may not exist,
            // or may not yet know whether it is on extended access (players not spawned yet).
            // PlayerJobsAssigned makes sure extended access is configured correctly in that case.

            var station = _stationSystem.GetOwningStation(id.Owner);
            var extended = false;
            if (station != null)
                extended = Comp<StationJobsComponent>(station.Value).ExtendedAccess;

            SetupIdAccess(uid, id, extended);
        }

        private void SetupIdAccess(EntityUid uid, PresetIdCardComponent id, bool extended)
        {
            if (id.JobName == null)
                return;

            if (!_prototypeManager.TryIndex(id.JobName, out JobPrototype? job))
            {
                Logger.ErrorS("access", $"Invalid job id ({id.JobName}) for preset card");
                return;
            }

            _accessSystem.SetAccessToJob(uid, job, extended);

            // and also change job title on a card id
            _cardSystem.TryChangeJobTitle(uid, job.LocalizedName);
        }
    }
}
