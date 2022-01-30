using System;
using System.Collections.Generic;
using System.Threading;
using Content.Shared.Cooldown;
using Content.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private HashSet<UseDelayComponent> _activeDelays = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new RemQueue<UseDelayComponent>();

        foreach (var delay in _activeDelays)
        {
            MetaDataComponent? metaData = null;

            if (Deleted(delay.Owner, metaData) ||
                delay.CancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                toRemove.Add(delay);
                continue;
            }

            if (Paused(delay.Owner, metaData)) continue;

            delay.Elapsed += frameTime;

            if (delay.Elapsed < delay.Delay) continue;

            toRemove.Add(delay);

        }

        foreach (var delay in toRemove)
        {
            delay.CancellationTokenSource = null;
            delay.Elapsed = 0f;
            _activeDelays.Remove(delay);
        }
    }

    public void BeginDelay(UseDelayComponent? component = null)
    {
        if (component == null ||
            component.ActiveDelay ||
            Deleted(component.Owner)) return;

        component.CancellationTokenSource = new CancellationTokenSource();

        DebugTools.Assert(!_activeDelays.Contains(component));
        _activeDelays.Add(component);

        var currentTime = _gameTiming.CurTime;
        component.LastUseTime = currentTime;

        var cooldown = EnsureComp<ItemCooldownComponent>(component.Owner);
        cooldown.CooldownStart = currentTime;
        cooldown.CooldownEnd = currentTime + TimeSpan.FromSeconds(component.Delay);
    }

    public void Cancel(UseDelayComponent component)
    {
        component.CancellationTokenSource?.Cancel();
        component.CancellationTokenSource = null;

        if (TryComp<ItemCooldownComponent>(component.Owner, out var cooldown))
        {
            cooldown.CooldownEnd = _gameTiming.CurTime;
        }
    }

    public void Restart(UseDelayComponent component)
    {
        component.CancellationTokenSource?.Cancel();
        component.CancellationTokenSource = null;
        BeginDelay(component);
    }
}
