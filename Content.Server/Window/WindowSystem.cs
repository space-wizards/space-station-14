using System;
using System.Linq;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Rounding;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Window;

public class WindowSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnExamine(EntityUid uid, WindowComponent component, ExaminedEvent args)
    {
        if (!TryComp(uid, out DamageableComponent? damageable) ||
            !TryComp(uid, out DestructibleComponent? destructible))
        {
            return;
        }

    }
}
