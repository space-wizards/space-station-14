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

    // On ignition, teleportation happens.
    private void OnIgnited(Entity<PaperQuantumComponent> entity, ref IgnitedEvent args)
    {
        // First, diisentangle both papers.
        if (!TryGetEntity(entity.Comp.Entangled, out var entangled))
            return;
        DisentangleOne((entity.Owner, entity.Comp));
        DisentangleOne(entangled.Value);

        // Then, create bluespace effect at source and destination.
        Spawn(entity.Comp.BluespaceEffectProto, entity.Owner.ToCoordinates());
        Spawn(entity.Comp.BluespaceEffectProto, entangled.Value.ToCoordinates());

        // Teleport items, up to the weight TeleportWeight.
        // TelportWeight halved on each faxing.
        var teleportWeight = entity.Comp.TeleportWeight;
        if (teleportWeight <= 0)
            return;
        var destination = _transform.GetMapCoordinates(entangled.Value);
        foreach (var nearEnt in _lookup.GetEntitiesInRange(entity.Owner.ToCoordinates(), 0.75f, LookupFlags.Dynamic | LookupFlags.Sundries)) // scan for items/mobs nearby
        {
            if (teleportWeight <= 0)
                break;
            if (nearEnt == entity.Owner || nearEnt == entangled) // don't teleport quantum papers.
                continue;
            if (TryComp(nearEnt, out ItemComponent? nearItem)) // if an item, try to teleport.
            {
                var weight = _item.GetItemSizeWeight(nearItem.Size);
                if (weight <= teleportWeight)
                {
                    teleportWeight -= weight;
                    _transform.SetMapCoordinates(nearEnt, destination);
                    _tempExplResist.ApplyResistance(nearEnt, TimeSpan.FromSeconds(0.5f)); // Add TemporaryExplosionResistant component
                    // to the teleported entity so they don't get instantly destroyed in the explosion. Doesn't save them from being ignited.
                }
            } else if (HasComp<BodyComponent>(nearEnt)) // if a mob with body, slice something off, reducing the channel capacity and damaging the entity.
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

        // Kaboom the entangled paper. Set fire damage it receives to 0 so it never actually burns.
        if (TryComp(entangled.Value, out FlammableComponent? entangledFlammable))
            entangledFlammable.Damage = new();
        _explosion.TriggerExplosive(entangled.Value);
   }
}
