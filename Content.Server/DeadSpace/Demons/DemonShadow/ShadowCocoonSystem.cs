// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Demons.DemonShadow.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Content.Server.Body.Components;
using Robust.Shared.Timing;
using Content.Shared.Destructible;
using Content.Server.DeadSpace.Abilities.Cocoon.Components;

namespace Content.Server.DeadSpace.Demons.LockCocoon;

public sealed class ShadowCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowCocoonComponent, BeingGibbedEvent>(OnGibbed);
        SubscribeLocalEvent<ShadowCocoonComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShadowCocoonComponent, ComponentShutdown>(OnShutDown);
        SubscribeLocalEvent<ShadowCocoonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadowCocoonComponent, DestructionEventArgs>(OnDestruction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shadowCocoonComponent = EntityQueryEnumerator<ShadowCocoonComponent>();
        while (shadowCocoonComponent.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime > component.NextTick)
            {
                TurnOffElectricity(uid, component);
            }
        }
    }
    private void TurnOffElectricity(EntityUid uid, ShadowCocoonComponent component)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        var entities = _lookup.GetEntitiesInRange<PointLightComponent>(_transform.GetMapCoordinates(uid, xform), component.Range);
        List<EntityUid> lights = new List<EntityUid>();

        foreach (var (entity, pointLightComp) in entities)
        {
            lights.Add(entity);
            _pointLightSystem.SetEnabled(entity, false);
            component.PointEntities.Add(entity);
        }

        foreach (var entity in component.PointEntities)
        {
            if (!lights.Contains(entity))
            {
                if (TryComp<PointLightComponent>(entity, out var poweredLight))
                {
                    _pointLightSystem.SetEnabled(entity, true);
                }
            }
        }

        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
    }
    private void OnInit(EntityUid uid, ShadowCocoonComponent component, ComponentInit args)
    {
        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
    }
    protected void OnMapInit(EntityUid uid, ShadowCocoonComponent component, MapInitEvent args)
    {
        if (!HasComp<CocoonComponent>(uid))
            AddComp<CocoonComponent>(uid);
    }

    private void OnGibbed(EntityUid uid, ShadowCocoonComponent component, BeingGibbedEvent args)
    {
        DestroyCocoon(uid, component);
    }
    private void OnShutDown(EntityUid uid, ShadowCocoonComponent component, ComponentShutdown args)
    {
        DestroyCocoon(uid, component);
    }

    private void OnDestruction(EntityUid uid, ShadowCocoonComponent component, DestructionEventArgs args)
    {
        DestroyCocoon(uid, component);
    }

    private void DestroyCocoon(EntityUid uid, ShadowCocoonComponent component)
    {
        foreach (var entity in component.PointEntities)
        {
            if (TryComp<PointLightComponent>(entity, out var poweredLight))
            {
                _pointLightSystem.SetEnabled(entity, true);
            }
        }
    }

}
