using Content.Client.Hands;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.SubFloor;

public sealed class TrayScannerSystem : SharedTrayScannerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO: Multiple viewports or w/e
        var player = _player.LocalPlayer?.ControlledEntity;
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(player, out var playerXform))
            return;

        var playerPos = _transform.GetWorldPosition(playerXform, xformQuery);
        var playerMap = playerXform.MapID;
        var range = 0f;

        if (TryComp<HandsComponent>(player, out var playerHands) &&
            TryComp<TrayScannerComponent>(playerHands.ActiveHandEntity, out var scanner) && scanner.Enabled)
        {
            range = scanner.Range;

            foreach (var comp in _lookup.GetComponentsInRange<SubFloorHideComponent>(playerMap, playerPos, range))
            {
                var uid = comp.Owner;
                if (!comp.IsUnderCover || !comp.BlockAmbience | !comp.BlockInteractions)
                    continue;

                EnsureComp<TrayRevealedComponent>(uid);
            }
        }

        var alphaChangeRate = 2f * frameTime;
        var revealedQuery = AllEntityQuery<TrayRevealedComponent, SpriteComponent, TransformComponent>();
        var subfloorQuery = GetEntityQuery<SubFloorHideComponent>();

        // TODO: Actually need to reveal the sprite dingus.
        while (revealedQuery.MoveNext(out var uid, out var revealed, out var sprite, out var xform))
        {
            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            // Revealing
            // Add buffer range to avoid flickers.
            if (subfloorQuery.HasComponent(uid) &&
                xform.MapID != MapId.Nullspace &&
                xform.MapID == playerMap &&
                xform.Anchored &&
                range != 0f &&
                (playerPos - worldPos).Length <= range + 0.5f)
            {
                var newAlpha = MathF.Min(SubfloorRevealAlpha, revealed.Alpha + alphaChangeRate);
                SetRevealed(uid, true);

                if (revealed.Alpha.Equals(newAlpha))
                    continue;

                sprite.Color = sprite.Color.WithAlpha(newAlpha);
                revealed.Alpha = newAlpha;
            }
            // Hiding
            else
            {
                var newAlpha = MathF.Max(0f, revealed.Alpha - alphaChangeRate);
                
                // Irrelevant
                if (revealed.Alpha.Equals(newAlpha))
                {
                    SetRevealed(uid, false);
                    RemCompDeferred<TrayRevealedComponent>(uid);
                    sprite.Color = sprite.Color.WithAlpha(1f);
                    continue;
                }

                SetRevealed(uid, true);
                sprite.Color = sprite.Color.WithAlpha(newAlpha);
                revealed.Alpha = newAlpha;
            }
        }
    }

    private void SetRevealed(EntityUid uid, bool value)
    {
        _appearance.SetData(uid, SubFloorVisuals.ScannerRevealed, value);
    }
}
