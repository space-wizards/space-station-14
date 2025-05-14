// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Factory;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._Goobstation.Factory;

/// <summary>
/// Animations robotic arm's arm layer swinging.
/// Can't be done with engine AnimationPlayer as it can't animate individual layers.
/// </summary>
public sealed class RoboticArmAnimationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<RoboticArmComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ItemSlot == null)
                continue;

            if (comp.NextMove is {} nextMove)
                Animate((uid, comp), nextMove);
            else
                Reset((uid, comp));
        }
    }

    private void Animate(Entity<RoboticArmComponent> ent, TimeSpan nextMove)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var started = nextMove - ent.Comp.MoveDelay;
        // 0-1 unless something weird happens
        var progress = (_timing.CurTime - started) / ent.Comp.MoveDelay;
        if (!ent.Comp.HasItem) // returning to the resting position when emptied
            progress = 1f - progress;
        var angle = Angle.FromDegrees(progress * 180f);
        sprite.LayerSetRotation(RoboticArmLayers.Arm, angle);
    }

    private void Reset(Entity<RoboticArmComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var angle = ent.Comp.HasItem ? new Angle(Math.PI) : Angle.Zero;
        sprite.LayerSetRotation(RoboticArmLayers.Arm, angle);
    }
}
