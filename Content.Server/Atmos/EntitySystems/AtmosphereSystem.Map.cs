using Content.Server.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    private void InitializeMap()
    {
        SubscribeLocalEvent<MapAtmosphereComponent, IsTileSpaceMethodEvent>(MapIsTileSpace);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixtureMethodEvent>(MapGetTileMixture);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixturesMethodEvent>(MapGetTileMixtures);
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

    private void MapGetTileMixtures(EntityUid uid, MapAtmosphereComponent component, ref GetTileMixturesMethodEvent args)
    {
        if (args.Handled || component.Mixture == null)
            return;
        args.Handled = true;
        args.Mixtures ??= new GasMixture?[args.Tiles.Count];

        for (var i = 0; i < args.Tiles.Count; i++)
        {
            args.Mixtures[i] ??= component.Mixture.Clone();
        }
    }
}
