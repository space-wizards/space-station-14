using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    private void InitializeMap()
    {
        SubscribeLocalEvent<MapAtmosphereComponent, ComponentStartup>(OnMapStartup);
        SubscribeLocalEvent<MapAtmosphereComponent, IsTileSpaceMethodEvent>(MapIsTileSpace);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixtureMethodEvent>(MapGetTileMixture);
        SubscribeLocalEvent<MapAtmosphereComponent, GetTileMixturesMethodEvent>(MapGetTileMixtures);
        SubscribeLocalEvent<MapAtmosphereComponent, ComponentGetState>(OnMapGetState);
    }

    private void OnMapStartup(EntityUid uid, MapAtmosphereComponent component, ComponentStartup args)
    {
        component.Mixture.MarkImmutable();
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

        args.Mixture = component.Mixture;
        args.Handled = true;
    }

    private void MapGetTileMixtures(EntityUid uid, MapAtmosphereComponent component, ref GetTileMixturesMethodEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        args.Mixtures ??= new GasMixture?[args.Tiles.Count];

        for (var i = 0; i < args.Tiles.Count; i++)
        {
            args.Mixtures[i] ??= component.Mixture;
        }
    }

    private void OnMapGetState(EntityUid uid, MapAtmosphereComponent component, ref ComponentGetState args)
    {
        args.State = new MapAtmosphereComponentState(component.Overlay);
    }

    public void SetMapAtmosphere(EntityUid uid, bool space, GasMixture mixture, MapAtmosphereComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetMapGasMixture(uid, mixture, component);
        SetMapSpace(uid, space, component);
    }

    public void SetMapGasMixture(EntityUid uid, GasMixture mixture, MapAtmosphereComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!mixture.Immutable)
        {
            mixture = mixture.Clone();
            mixture.MarkImmutable();
        }

        component.Mixture = mixture;
        component.Overlay = _gasTileOverlaySystem.GetOverlayData(component.Mixture);
        Dirty(uid, component);
    }

    public void SetMapSpace(EntityUid uid, bool space, MapAtmosphereComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Space = space;
    }
}
