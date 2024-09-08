using Content.Shared.Coordinates;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Server.Teleportation;
using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Server.Paper;

public sealed class PaperQuantumSystem : SharedPaperQuantumSystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TemporaryExplosionResistantSystem _tempExplResist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperQuantumComponent, IgnitedEvent>(OnIgnited);
    }

    protected override void DisentangleOne(Entity<PaperQuantumComponent?> entity)
    {
        base.DisentangleOne(entity);
        RemCompDeferred<SuperposedComponent>(entity);
    }

    private void OnIgnited(Entity<PaperQuantumComponent> entity, ref IgnitedEvent args)
    {
        // Disentangle
        if (!TryGetEntity(entity.Comp.Entangled, out var entangled))
            return;
        DisentangleOne((entity.Owner, entity.Comp));
        DisentangleOne(entangled.Value);

        Spawn(entity.Comp.BluespaceStampEffectProto, entity.Owner.ToCoordinates());
        Spawn(entity.Comp.BluespaceStampEffectProto, entangled.Value.ToCoordinates());

        var teleportWeight = entity.Comp.TeleportWeight;
        if (teleportWeight <= 0)
            return;
        var destination = _transform.GetMapCoordinates(entangled.Value);
        foreach (var nearEnt in _lookup.GetEntitiesInRange(entity.Owner.ToCoordinates(), 1f, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (teleportWeight <= 0)
                break;
            if (nearEnt == entity.Owner || nearEnt == entangled)
                continue;
            if (TryComp(nearEnt, out ItemComponent? nearItem))
            {
                var weight = _item.GetItemSizeWeight(nearItem.Size);
                if (weight <= teleportWeight)
                {
                    teleportWeight -= weight;
                    _transform.SetMapCoordinates(nearEnt, destination);
                    _tempExplResist.ApplyResistance(nearEnt, TimeSpan.FromSeconds(0.5f));
                }
            } else if (HasComp<BodyComponent>(nearEnt))
            {
                teleportWeight -= 1;
                _damage.TryChangeDamage(nearEnt, entity.Comp.Damage);
                _popup.PopupEntity(Loc.GetString(entity.Comp.PopupTraumaSelf), nearEnt, nearEnt, PopupType.MediumCaution);
                _popup.PopupEntity(
                    Loc.GetString(entity.Comp.PopupTraumaOther, ("user", Identity.Entity(nearEnt, EntityManager))),
                    nearEnt,
                    Filter.PvsExcept(nearEnt),
                    true,
                    PopupType.Medium
                );
            }
        }

        if (TryComp(entangled.Value, out FlammableComponent? entangledFlammable))
            entangledFlammable.Damage = new();
        _explosion.TriggerExplosive(entangled.Value);
   }
}
