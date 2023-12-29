using Content.Server.Salvage.Magnet;
using Content.Shared.Salvage.Magnet;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void InitializeMagnet()
    {
        SubscribeLocalEvent<SalvageMagnetDataComponent, MapInitEvent>(OnMagnetDataMapInit);

        SubscribeLocalEvent<SalvageMagnetTargetComponent, GridSplitEvent>(OnMagnetTargetSplit);

        SubscribeLocalEvent<SalvageMagnetComponent, MagnetClaimOfferEvent>(OnMagnetClaim);
        SubscribeLocalEvent<SalvageMagnetComponent, ComponentStartup>(OnMagnetStartup);
        SubscribeLocalEvent<SalvageMagnetComponent, AnchorStateChangedEvent>(OnMagnetAnchored);
    }

    private void OnMagnetClaim(EntityUid uid, SalvageMagnetComponent component, ref MagnetClaimOfferEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player is null)
            return;

        var station = _station.GetOwningStation(uid);

        if (!TryComp(station, out SalvageMagnetDataComponent? dataComp) ||
            dataComp.NextOffer > _timing.CurTime)
        {
            return;
        }

        TakeMagnetOffer((station.Value, dataComp), args.Index, uid);
    }

    private void OnMagnetStartup(EntityUid uid, SalvageMagnetComponent component, ComponentStartup args)
    {
        UpdateMagnetUI((uid, component), Transform(uid));
    }

    private void OnMagnetAnchored(EntityUid uid, SalvageMagnetComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        UpdateMagnetUI((uid, component), args.Transform);
    }

    private void OnMagnetDataMapInit(EntityUid uid, SalvageMagnetDataComponent component, ref MapInitEvent args)
    {
        CreateMagnetOffers((uid, component));
    }

    private void OnMagnetTargetSplit(EntityUid uid, SalvageMagnetTargetComponent component, ref GridSplitEvent args)
    {
        // Don't think I'm not onto you people splitting to make new grids.
        if (TryComp(component.DataTarget, out SalvageMagnetDataComponent? dataComp))
        {
            foreach (var gridUid in args.NewGrids)
            {
                dataComp.ActiveEntities?.Add(gridUid);
            }
        }
    }

    private void UpdateMagnet()
    {
        var dataQuery = EntityQueryEnumerator<SalvageMagnetDataComponent>();
        var curTime = _timing.CurTime;

        while (dataQuery.MoveNext(out var uid, out var magnetData))
        {
            // Magnet currently active.
            if (magnetData.EndTime != null)
            {
                if (magnetData.EndTime.Value < curTime)
                {
                    EndMagnet((uid, magnetData));
                }
            }

            if (magnetData.NextOffer < curTime)
            {
                CreateMagnetOffers((uid, magnetData));
            }
        }
    }

    private void EndMagnet(Entity<SalvageMagnetDataComponent> data)
    {
        if (data.Comp.ActiveEntities != null)
        {
            foreach (var ent in data.Comp.ActiveEntities)
            {
                QueueDel(ent);
            }

            data.Comp.ActiveEntities = null;
        }

        data.Comp.EndTime = null;
        UpdateMagnetUIs(data);
    }

    private void CreateMagnetOffers(Entity<SalvageMagnetDataComponent> data)
    {
        data.Comp.Offered.Clear();

        for (var i = 0; i < data.Comp.OfferCount; i++)
        {
            var seed = _random.Next();

            // Fuck with the seed to mix wrecks and asteroids.
            seed = (int) (seed / 10f) * 10;

            if (i >= data.Comp.OfferCount / 2)
            {
                seed++;
            }

            data.Comp.Offered.Add(seed);
        }

        data.Comp.NextOffer = _timing.CurTime + data.Comp.OfferCooldown;
        UpdateMagnetUIs(data);
    }

    private void UpdateMagnetUI(Entity<SalvageMagnetComponent> entity, TransformComponent xform)
    {
        var station = _station.GetOwningStation(entity, xform);

        if (!TryComp(station, out SalvageMagnetDataComponent? dataComp))
            return;

        _ui.TrySetUiState(entity, SalvageMagnetUiKey.Key,
            new SalvageMagnetBoundUserInterfaceState(dataComp.Offered)
            {
                Cooldown = dataComp.OfferCooldown,
                Duration = dataComp.ActiveTime,
                EndTime = dataComp.EndTime,
                NextOffer = dataComp.NextOffer
            });
    }

    private void UpdateMagnetUIs(Entity<SalvageMagnetDataComponent> data)
    {
        var query = AllEntityQuery<SalvageMagnetComponent, TransformComponent>();

        while (query.MoveNext(out var magnetUid, out var magnet, out var xform))
        {
            var station = _station.GetOwningStation(magnetUid, xform);

            if (station != data.Owner)
                continue;

            _ui.TrySetUiState(magnetUid, SalvageMagnetUiKey.Key,
                new SalvageMagnetBoundUserInterfaceState(data.Comp.Offered)
                {
                    Cooldown = data.Comp.OfferCooldown,
                    Duration = data.Comp.ActiveTime,
                    EndTime = data.Comp.EndTime,
                    NextOffer = data.Comp.NextOffer
                });
        }
    }

    public void TakeMagnetOffer(Entity<SalvageMagnetDataComponent> data, int index, EntityUid magnet)
    {
        var seed = data.Comp.Offered[index];

        var offering = GetSalvageOffering(seed);

        // TODO: Old code

        data.Comp.EndTime = _timing.CurTime + data.Comp.ActiveTime;
        data.Comp.NextOffer = data.Comp.EndTime.Value + data.Comp.OfferCooldown;
        var active = new SalvageMagnetActivatedEvent()
        {
            Magnet = magnet,
        };

        RaiseLocalEvent(ref active);
        UpdateMagnetUIs(data);
    }
}

[ByRefEvent]
public record struct SalvageMagnetActivatedEvent
{
    public EntityUid Magnet;
}
