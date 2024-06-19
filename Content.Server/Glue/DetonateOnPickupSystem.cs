using Content.Server.Explosion.EntitySystems;
using Content.Shared.Glue;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;

namespace Content.Server.Glue;

public sealed class DetonateOnPickupSystem : EntitySystem
{
  [Dependency] private readonly SharedPopupSystem _popup = default!;
  [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
  [Dependency] private readonly NpcFactionSystem _factionSystem = default!;

  public override void Initialize()
  {
    base.Initialize();
    
    SubscribeLocalEvent<DetonateOnPickupComponent, GotEquippedHandEvent>(OnHandPickUp);
  }

  private void OnHandPickUp(Entity<DetonateOnPickupComponent> entity, ref GotEquippedHandEvent args)
  {
    if(!_factionSystem.IsMember(args.User, "Syndicate"))
    {
      var comp = EnsureComp<UnremoveableComponent>(entity);
      comp.DeleteOnDrop = false;
      _explosionSystem.QueueExplosion(entity, "MicroBomb", 200f, 200f, 200f, canCreateVacuum: true);
    }
  }
}
