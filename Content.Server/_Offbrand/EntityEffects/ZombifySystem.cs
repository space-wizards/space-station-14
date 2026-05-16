using Content.Server.Zombies;
using Content.Shared._Offbrand.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server._Offbrand.EntityEffects;

public sealed partial class ZombifySystem : EntityEffectSystem<MetaDataComponent, Zombify>
{
    [Dependency] private ZombieSystem _zombie = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<Zombify> args)
    {
        _zombie.ZombifyEntity(ent);
    }
}
