// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class NecroobeliskArtefactRuleSystem : GameRuleSystem<NecroobeliskArtefactRuleComponent>
{
    private static readonly ResPath ObeliskOrderPath = new("/Paperwork/StationGoal/Obelisk.xml");

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, NecroobeliskArtefactRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.TimeUntilStart = _timing.CurTime + component.StartDuration;
    }

    protected override void ActiveTick(EntityUid uid, NecroobeliskArtefactRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (_timing.CurTime >= component.TimeUntilStart && !component.IsArtefactSended)
            StartRule(uid);

        return;
    }

    private void StartRule(EntityUid uid, NecroobeliskArtefactRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        GameTicker.StartGameRule("GiftsNecroobeliskArtefact");
        component.IsArtefactSended = true;

        var faxes = EntityQueryEnumerator<FaxMachineComponent>();
        var contentTemplate = _resourceManager.ContentFileReadText(ObeliskOrderPath).ReadToEnd();

        while (faxes.MoveNext(out var faxEnt, out var fax))
        {
            if (!fax.ReceiveNukeCodes)
                continue;

            var content = contentTemplate;

            if (_station.GetOwningStation(faxEnt) is { } station)
                content = content.Replace("STATION XX-00", Name(station));

            var printout = new FaxPrintout(
                content,
                Loc.GetString("paper-order-name"),
                null,
                prototypeId: "PaperPrintedCentcomm",
                "paper_stamp-centcom",
                new List<StampDisplayInfo>
                {
                    new StampDisplayInfo
                    {
                        StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                        StampedColor = Color.FromHex("#006600"),
                        StampTexture = "/Textures/Interface/Stamps/centralcommand_print.png",
                        StampScale = 0.92f,
                    },
                },
                signatures: new List<string> { "Эвелин Маршалл" }
            );

            _faxSystem.Receive(faxEnt, printout, null, fax);
        }
    }
}
