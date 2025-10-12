using Content.Server.Zombies;
using Content.Shared._Offbrand.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server._Offbrand.EntityEffects;

public sealed class ZombifySystem : EntitySystem
{
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecuteEntityEffectEvent<Zombify>>(OnExecuteZombify);
    }

    private void OnExecuteZombify(ref ExecuteEntityEffectEvent<Zombify> args)
    {
        _zombie.ZombifyEntity(args.Args.TargetEntity);
    }
}
