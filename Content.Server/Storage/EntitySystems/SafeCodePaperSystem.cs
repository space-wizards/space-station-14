using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Content.Shared.Station.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Lock;
using System.Text;
using Content.Server.Pinpointer;

namespace Content.Server.Storage
{
    public sealed class SafeCodePaperSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SafeCodePaperComponent, MapInitEvent>(OnMapInit,
                after: new[] { typeof(RotaryLockSystem) });
        }

        private void OnMapInit(EntityUid uid, SafeCodePaperComponent component, MapInitEvent args)
        {
            SetupPaper(uid, component);
        }

        private void SetupPaper(EntityUid uid, SafeCodePaperComponent? component = null, EntityUid? station = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (TryGetRelativeNukeCode(uid, out var paperContent, station))
            {
                if (TryComp<PaperComponent>(uid, out var paperComp))
                {
                    _paper.SetContent((uid, paperComp), paperContent);

                    StampDisplayInfo stamp = new StampDisplayInfo { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#BB3232") };
                    _paper.TryStamp((uid, paperComp), stamp, "paper_stamp-centcom");
                }
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
            EntityUid? station = null)
        {
            nukeCode = null;

            var owningStation = station ?? _station.GetOwningStation(uid);

            var codesMessage = new FormattedMessage();
            codesMessage.PushNewline();

            var safes = new List<Entity<RotaryLockComponent>>();
            var query = EntityQueryEnumerator<RotaryLockComponent>();
            while (query.MoveNext(out var safeUid, out var safe))
            {
                safes.Add((safeUid, safe));
            }

            foreach (var (safeUid, safe) in safes)
            {
                string code = string.Join(", ", safe.Tumblers.ToArray());

                string location = FormattedMessage.RemoveMarkupOrThrow(
                    _navMap.GetNearestBeaconString((safeUid, Transform(safeUid)), true));

                codesMessage.PushNewline();
                codesMessage.AddMarkupOrThrow(Loc.GetString("safe-codes-list", ("location", location), ("code", code)));
            }

            if (!codesMessage.IsEmpty)
                nukeCode = Loc.GetString("safe-codes-message") + codesMessage;
            return !codesMessage.IsEmpty;
        }
    }
}
