using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    private void InitializeMap()
    {
        SubscribeLocalEvent<MapAtmosphereComponent, IsTileSpaceMethodEvent>(MapIsTileSpace);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixtureMethodEvent>(MapGetTileMixture);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixturesMethodEvent>(MapGetTileMixtures);
        SubscribeLocalEvent<MapAtmosphereComponent, ComponentGetState>(OnMapGetState);
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

    private void OnMapGetState(EntityUid uid, MapAtmosphereComponent component, ref ComponentGetState args)
    {
        args.State = new MapAtmosphereComponentState(_gasTileOverlaySystem.GetOverlayData(component.Mixture));
    }

    public void SetMapAtmosphere(EntityUid uid, bool space, GasMixture mixture, MapAtmosphereComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Space = space;
        component.Mixture = mixture;
        Dirty(component);
    }

    public void SetMapGasMixture(EntityUid uid, GasMixture? mixture, MapAtmosphereComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Mixture = mixture;
        Dirty(component);
    }

    public void SetMapSpace(EntityUid uid, bool space, MapAtmosphereComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Space = space;
        Dirty(component);
    }
}
