using Content.Server.Storage.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Storage.EntitySystems;

public sealed class InternalAirSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalAirComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnPriceCalculation(EntityUid uid, InternalAirComponent component, ref PriceCalculationEvent args)
    {
        args.Price += _atmosphere.GetPrice(component.Air);
    }
}
