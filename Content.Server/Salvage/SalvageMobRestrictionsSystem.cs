using Content.Shared.Damage;
using Content.Server.Body.Components;

namespace Content.Server.Salvage;

public sealed class SalvageMobRestrictionsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SalvageMobRestrictionsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SalvageMobRestrictionsComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SalvageMobRestrictionsGridComponent, ComponentRemove>(OnRemoveGrid);
    }

    private void OnInit(EntityUid uid, SalvageMobRestrictionsComponent component, ComponentInit args)
    {
        var gridUid = Transform(uid).ParentUid;
        if (!EntityManager.EntityExists(gridUid))
        {
            // Give up, we were spawned improperly
            return;
        }
        // When this code runs, the salvage magnet hasn't actually gotten ahold of the entity yet.
        // So it therefore isn't in a position to do this.
        if (!TryComp(gridUid, out SalvageMobRestrictionsGridComponent? rg))
        {
            rg = AddComp<SalvageMobRestrictionsGridComponent>(gridUid);
        }
        rg.MobsToKill.Add(uid);
        component.LinkedGridEntity = gridUid;
    }

    private void OnRemove(EntityUid uid, SalvageMobRestrictionsComponent component, ComponentRemove args)
    {
        if (TryComp(component.LinkedGridEntity, out SalvageMobRestrictionsGridComponent? rg))
        {
            rg.MobsToKill.Remove(uid);
        }
    }

    private void OnRemoveGrid(EntityUid uid, SalvageMobRestrictionsGridComponent component, ComponentRemove args)
    {
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var bodyQuery = GetEntityQuery<BodyComponent>();
        var damageQuery = GetEntityQuery<DamageableComponent>();
        foreach (var target in component.MobsToKill)
        {
            if (Deleted(target, metaQuery)) continue;
            if (bodyQuery.TryGetComponent(target, out var body))
            {
                // Just because.
                body.Gib();
            }
            else if (damageQuery.TryGetComponent(target, out var damageableComponent))
            {
                _damageableSystem.SetAllDamage(damageableComponent, 200);
            }
        }
    }
}

