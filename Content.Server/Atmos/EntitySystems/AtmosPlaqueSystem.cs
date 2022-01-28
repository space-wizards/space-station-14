using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Visuals;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

public sealed class AtmosPlaqueSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmosPlaqueComponent, ComponentStartup>(OnPlaqueStartup);
        SubscribeLocalEvent<AtmosPlaqueComponent, MapInitEvent>(OnPlaqueMapInit);
    }

    private void OnPlaqueStartup(EntityUid uid, AtmosPlaqueComponent component, ComponentStartup args)
    {
        UpdateSign(component);
    }

    private void OnPlaqueMapInit(EntityUid uid, AtmosPlaqueComponent component, MapInitEvent args)
    {
        var rand = _random.Next(100);
        // Let's not pat ourselves on the back too hard.
        // 1% chance of zumos
        if (rand == 0) component.TypeVV = PlaqueType.Zumos;
        // 9% FEA
        else if (rand <= 10) component.TypeVV = PlaqueType.Fea;
        // 45% ZAS
        else if (rand <= 55) component.TypeVV = PlaqueType.Zas;
        // 45% LINDA
        else component.TypeVV = PlaqueType.Linda;
    }

    public void UpdateSign(AtmosPlaqueComponent component)
    {
        var metaData = MetaData(component.Owner);

        var val = component.Type switch
        {
            PlaqueType.Zumos =>
                "This plaque commemorates the rise of the Atmos ZUM division. May they carry the torch that the Atmos ZAS, LINDA and FEA divisions left behind.",
            PlaqueType.Fea =>
                "This plaque commemorates the fall of the Atmos FEA division. For all the charred, dizzy, and brittle men who have died in its hands.",
            PlaqueType.Linda =>
                "This plaque commemorates the fall of the Atmos LINDA division. For all the charred, dizzy, and brittle men who have died in its hands.",
            PlaqueType.Zas =>
                "This plaque commemorates the fall of the Atmos ZAS division. For all the charred, dizzy, and brittle men who have died in its hands.",
            PlaqueType.Unset => "Uhm",
            _ => "Uhm",
        };

        metaData.EntityDescription = val;

        var val1 = component.Type switch
        {
            PlaqueType.Zumos =>
                "ZUM Atmospherics Division plaque",
            PlaqueType.Fea =>
                "FEA Atmospherics Division plaque",
            PlaqueType.Linda =>
                "LINDA Atmospherics Division plaque",
            PlaqueType.Zas =>
                "ZAS Atmospherics Division plaque",
            PlaqueType.Unset => "Uhm",
            _ => "Uhm",
        };

        metaData.EntityName = val1;

        if (TryComp<AppearanceComponent>(component.Owner, out var appearance))
        {
            var state = component.Type == PlaqueType.Zumos ? "zumosplaque" : "atmosplaque";

            appearance.SetData(AtmosPlaqueVisuals.State, state);
        }
    }
}

// If you get the ZUM plaque it means your round will be blessed with good engineering luck.
public enum PlaqueType : byte
{
    Unset = 0,
    Zumos,
    Fea,
    Linda,
    Zas
}
