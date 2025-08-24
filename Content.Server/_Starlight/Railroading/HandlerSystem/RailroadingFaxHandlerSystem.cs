using Content.Server.Fax;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Fax.Components;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingFaxHandlerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadFaxOnChosenComponent, RailroadingCardChosenEvent>(OnChosen);
    }

    private void OnChosen(Entity<RailroadFaxOnChosenComponent> ent, ref RailroadingCardChosenEvent args)
    {
        var letter = _random.Pick(ent.Comp.Letters);

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = FaxConstants.FaxPrintCommand,
            [FaxConstants.FaxPaperNameData] = Loc.GetString(letter.PaperName, ("subject", args.Subject)),
            [FaxConstants.FaxPaperContentData] = Loc.GetString(letter.PaperContent, ("subject",args.Subject)),
            [FaxConstants.FaxPaperLabelData] = letter.PaperLabel,
            [FaxConstants.FaxPaperStampStateData] = letter.StampState,
            [FaxConstants.FaxPaperStampedByData] = letter.StampedBy,
            [FaxConstants.FaxPaperPrototypeData] = letter.PaperPrototype,
            [FaxConstants.FaxPaperLockedData] = letter.Locked
        };

        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var faxEnt, out var faxComp))
        {
            if (!ent.Comp.Addresses.Contains(faxComp.FaxName))
                continue;
            var @event = new DeviceNetworkPacketEvent
                (
                    0,
                    null,
                    0,
                    "NT",
                    EntityUid.Invalid,
                    payload
                );
            RaiseLocalEvent(faxEnt, @event);
        }
    }
}
