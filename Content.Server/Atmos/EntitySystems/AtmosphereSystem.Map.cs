using Content.Server.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    private void InitializeMap()
    {
        SubscribeLocalEvent<MapAtmosphereComponent, IsTileSpaceMethodEvent>(MapIsTileSpace);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixtureMethodEvent>(MapGetTileMixture);
    }

    private void MapIsTileSpace(EntityUid uid, MapAtmosphereComponent component, ref IsTileSpaceMethodEvent args)
    {
        if (args.Handled)
            return;

        args.Result = component.Space;
        args.Handled = true;
    }

    private void MapGetTileMixture(EntityUid uid, MapAtmosphereComponent component, ref GetTileMixtureMethodEvent args)
    {
        if (args.Handled)
            return;

        // Clone the mixture, if possible.
        args.Mixture = component.Mixture?.Clone();
        args.Handled = true;
    }
}
