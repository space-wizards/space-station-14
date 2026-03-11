using Content.Shared.Bible;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Bible;

public sealed class BlessedVisualizerSystem : VisualizerSystem<BlessedVisualsComponent>
{
    [Dependency] private readonly PointLightSystem _lightSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlessedVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BlessedVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnComponentInit(Entity<BlessedVisualsComponent> entity, ref ComponentInit args)
    {
        UpdateAppearance(entity);
    }

    protected override void OnAppearanceChange(EntityUid entity, BlessedVisualsComponent component, ref AppearanceChangeEvent args)
    {
        UpdateAppearance((entity, component));
    }

    private void UpdateAppearance(Entity<BlessedVisualsComponent> entity)
    {
        if (AppearanceSystem.TryGetData<bool>(entity, BlessedVisuals.HolyLight, out var holyLight))
        {
            if (holyLight)
            {
                entity.Comp.LightEntity ??= Spawn(null, new EntityCoordinates(entity.Owner, default));
                var light = EnsureComp<PointLightComponent>(entity.Comp.LightEntity.Value);

                _lightSystem.SetColor(entity.Comp.LightEntity.Value, entity.Comp.LightColor, light);
                _lightSystem.SetRadius(entity.Comp.LightEntity.Value, entity.Comp.LightRadius, light);
                _lightSystem.SetEnergy(entity.Comp.LightEntity.Value, entity.Comp.LightEnergy, light);
            }
            else if (entity.Comp.LightEntity.HasValue)
            {
                Del(entity.Comp.LightEntity);
                entity.Comp.LightEntity = null;
            }
        }
    }

    private void OnShutdown(Entity<BlessedVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.LightEntity != null)
        {
            Del(ent.Comp.LightEntity.Value);
            ent.Comp.LightEntity = null;
        }
    }
}
