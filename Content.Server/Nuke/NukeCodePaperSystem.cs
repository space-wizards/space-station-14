using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Nuke
{
    public sealed class NukeCodePaperSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeCodePaperComponent, MapInitEvent>(OnMapInit,
                after: new []{ typeof(NukeLabelSystem) });
        }

        private void OnMapInit(EntityUid uid, NukeCodePaperComponent component, MapInitEvent args)
        {
            SetupPaper(uid, component);
        }

        private void SetupPaper(EntityUid uid, NukeCodePaperComponent? component = null, EntityUid? station = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (TryGetRelativeNukeCode(uid, out var paperContent, station, onlyCurrentStation: component.AllNukesAvailable))
            {
                if (TryComp<PaperComponent>(uid, out var paperComp))
                    _paper.SetContent((uid, paperComp), paperContent);
            }
        }

        /// <summary>
        ///     Send a nuclear code to all faxes on that station which are authorized to receive nuke codes.
        /// </summary>
        /// <returns>True if at least one fax received codes</returns>
        public bool SendNukeCodes(EntityUid station)
        {
            if (!HasComp<StationDataComponent>(station))
            {
                return false;
            }

            var faxes = EntityQueryEnumerator<FaxMachineComponent>();
            var wasSent = false;
            while (faxes.MoveNext(out var faxEnt, out var fax))
            {
                if (!fax.ReceiveNukeCodes || !TryGetRelativeNukeCode(faxEnt, out var paperContent, station))
                {
                    continue;
                }

                var printout = new FaxPrintout(
                    paperContent,
                    Loc.GetString("nuke-codes-fax-paper-name"),
                    null,
                    null,
                    "paper_stamp-centcom",
                    new List<StampDisplayInfo>
                    {
                        new StampDisplayInfo { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#BB3232") },
                    }
                );
                _faxSystem.Receive(faxEnt, printout, null, fax);

                wasSent = true;
            }

            if (wasSent)
            {
                var msg = Loc.GetString("nuke-component-announcement-send-codes");
                _chatSystem.DispatchStationAnnouncement(station, msg, colorOverride: Color.Red);
            }

            return wasSent;
        }

        private bool TryGetRelativeNukeCode(
            EntityUid uid,
            [NotNullWhen(true)] out string? nukeCode,
            EntityUid? station = null,
            TransformComponent? transform = null,
            bool onlyCurrentStation = false)
        {
            nukeCode = null;
            if (!Resolve(uid, ref transform))
            {
                return false;
            }

            var owningStation = station ?? _station.GetOwningStation(uid);

            var codesMessage = new FormattedMessage();
            // Find the first nuke that matches the passed location.
            var nukes = new List<Entity<NukeComponent>>();
            var query = EntityQueryEnumerator<NukeComponent>();
            while (query.MoveNext(out var nukeUid, out var nuke))
            {
                nukes.Add((nukeUid, nuke));
            }

            _random.Shuffle(nukes);

            foreach (var (nukeUid, nuke) in nukes)
            {
                if (!onlyCurrentStation &&
                    (owningStation == null &&
                    nuke.OriginMapGrid != (transform.MapID, transform.GridUid) ||
                    nuke.OriginStation != owningStation))
                {
                    continue;
                }

                codesMessage.PushNewline();
                codesMessage.AddMarkup(Loc.GetString("nuke-codes-list", ("name", MetaData(nukeUid).EntityName), ("code", nuke.Code)));
                break;
            }

            if (!codesMessage.IsEmpty)
                nukeCode = Loc.GetString("nuke-codes-message")+codesMessage;
            return !codesMessage.IsEmpty;
        }
    }
}
