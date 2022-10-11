using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Paper;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;

namespace Content.Server.Nuke
{
    public sealed class NukeCodePaperSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly StationSystem _station = default!;

        private const string NukePaperPrototype = "NukeCodePaper";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeCodePaperComponent, MapInitEvent>(OnMapInit,
                after: new []{ typeof(NukeLabelSystem) });
        }

        private void OnMapInit(EntityUid uid, NukeCodePaperComponent component, MapInitEvent args)
        {
            SetupPaper(uid);
        }

        private void SetupPaper(EntityUid uid, EntityUid? station = null, PaperComponent? paper = null)
        {
            if (!Resolve(uid, ref paper))
            {
                return;
            }

            var owningStation = station ?? _station.GetOwningStation(uid);
            var transform = Transform(uid);

            // Find the first nuke that matches the paper's location.
            foreach (var nuke in EntityQuery<NukeComponent>())
            {
                if (owningStation == null && nuke.OriginMapGrid != (transform.MapID, transform.GridUid)
                    || nuke.OriginStation != owningStation)
                {
                    continue;
                }

                paper.Content += $"{MetaData(nuke.Owner).EntityName} - {nuke.Code}";
                break;
            }
        }

        /// <summary>
        ///     Send a nuclear code to all communication consoles
        /// </summary>
        /// <returns>True if at least one console received codes</returns>
        public bool SendNukeCodes(EntityUid station)
        {
            if (!HasComp<StationDataComponent>(station))
            {
                return false;
            }

            // todo: this should probably be handled by fax system
            var wasSent = false;
            var consoles = EntityQuery<CommunicationsConsoleComponent, TransformComponent>();
            foreach (var (console, transform) in consoles)
            {
                var owningStation = _station.GetOwningStation(console.Owner);
                if (owningStation == null || owningStation != station)
                {
                    continue;
                }

                var consolePos = transform.MapPosition;
                var uid = Spawn(NukePaperPrototype, consolePos);
                SetupPaper(uid, station);

                wasSent = true;
            }

            if (wasSent)
            {
                var msg = Loc.GetString("nuke-component-announcement-send-codes");
                _chatSystem.DispatchStationAnnouncement(station, msg, colorOverride: Color.Red);
            }

            return wasSent;
        }
    }
}
