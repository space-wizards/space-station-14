using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

public sealed class AtmosPlaqueSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmosPlaqueComponent, MapInitEvent>(OnPlaqueMapInit);
    }

    private void OnPlaqueMapInit(EntityUid uid, AtmosPlaqueComponent component, MapInitEvent args)
    {
        var rand = _random.Next(100);
        // Let's not pat ourselves on the back too hard.
        // 1% chance of zumos
        if (rand == 0) component.Type = PlaqueType.Zumos;
        // 9% FEA
        else if (rand <= 10) component.Type = PlaqueType.Fea;
        // 45% ZAS
        else if (rand <= 55) component.Type = PlaqueType.Zas;
        // 45% LINDA
        else component.Type = PlaqueType.Linda;

        UpdateSign(uid, component);
    }

    public void UpdateSign(EntityUid uid, AtmosPlaqueComponent component)
    {
        var metaData = MetaData(uid);

        var val = component.Type switch
        {
            PlaqueType.Zumos =>
                Loc.GetString("atmos-plaque-component-desc-zum"),
            PlaqueType.Fea =>
                Loc.GetString("atmos-plaque-component-desc-fea"),
            PlaqueType.Linda =>
                Loc.GetString("atmos-plaque-component-desc-linda"),
            PlaqueType.Zas =>
                Loc.GetString("atmos-plaque-component-desc-zas"),
            PlaqueType.Unset => Loc.GetString("atmos-plaque-component-desc-unset"),
            _ => Loc.GetString("atmos-plaque-component-desc-unset"),
        };

        _metaData.SetEntityDescription(uid, val, metaData);

        var val1 = component.Type switch
        {
            PlaqueType.Zumos =>
                Loc.GetString("atmos-plaque-component-name-zum"),
            PlaqueType.Fea =>
                Loc.GetString("atmos-plaque-component-name-fea"),
            PlaqueType.Linda =>
                Loc.GetString("atmos-plaque-component-name-linda"),
            PlaqueType.Zas =>
                Loc.GetString("atmos-plaque-component-name-zas"),
            PlaqueType.Unset => Loc.GetString("atmos-plaque-component-name-unset"),
            _ => Loc.GetString("atmos-plaque-component-name-unset"),
        };

        _metaData.SetEntityName(uid, val1, metaData);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            var state = component.Type == PlaqueType.Zumos ? "zumosplaque" : "atmosplaque";

            _appearance.SetData(uid, AtmosPlaqueVisuals.State, state, appearance);
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
