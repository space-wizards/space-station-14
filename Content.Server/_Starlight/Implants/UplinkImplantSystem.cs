using Content.Shared._Starlight.Implants.Components;
using Content.Shared.Implants;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;

namespace Content.Server._Starlight.Implants;

public sealed class UplinkImplantSystem : EntitySystem
{
    [Dependency] private readonly UplinkSystem _uplink = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(EntityUid uid, UplinkImplantComponent component, ref ImplantImplantedEvent args) => _uplink.SetUplink(args.Implanted, uid, FixedPoint2.New(0), true);
}