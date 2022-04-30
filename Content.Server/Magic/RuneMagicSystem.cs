using System.Text;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.ReagentEffects;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Magic;

public class RuneMagicSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

    }


}
