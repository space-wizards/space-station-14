using Robust.Client.GameObjects;

namespace Content.Client.Wall;

/// <summary>
/// Tracks fade progress for a wall-mounted entity.
/// </summary>
internal struct WallMountFadeState
{
    public float OriginalAlpha;
    public float CurrentAlpha;
    public float TargetAlpha;

    /// <summary>
    /// Sprite alpha after applying the fade multiplier.
    /// </summary>
    public readonly float EffectiveAlpha => OriginalAlpha * CurrentAlpha;

    /// <summary>
    /// Whether the entity should be rendered as visible at the current fade level.
    /// </summary>
    public readonly bool IsVisible => CurrentAlpha > 0f;

    /// <summary>
    /// Creates a new fade state snapped immediately to the given target alpha.
    /// </summary>
    public static WallMountFadeState Snapped(float originalAlpha, float targetAlpha)
    {
        return new()
        {
            OriginalAlpha = originalAlpha,
            CurrentAlpha = targetAlpha,
            TargetAlpha = targetAlpha,
        };
    }

    /// <summary>
    /// Moves <see cref="CurrentAlpha"/> one step towards <see cref="TargetAlpha"/>.
    /// </summary>
    public void StepTowards(float fadeStep)
    {
        if (MathHelper.CloseTo(CurrentAlpha, TargetAlpha))
            return;

        var delta = TargetAlpha - CurrentAlpha;
        var step = MathF.Sign(delta) * MathF.Min(MathF.Abs(delta), fadeStep);
        CurrentAlpha = Math.Clamp(CurrentAlpha + step, 0f, 1f);
    }
}

/// <summary>
/// Per-viewport fade state for wall-mounted entities.
/// </summary>
internal sealed class ViewportFadeState(SpriteSystem sprite, EntityQuery<SpriteComponent> spriteQuery) : IDisposable
{
    private readonly SpriteSystem _sprite = sprite;
    private readonly EntityQuery<SpriteComponent> _spriteQuery = spriteQuery;

    /// <summary>
    /// Fade states for entities tracked in this viewport.
    /// </summary>
    public readonly Dictionary<EntityUid, WallMountFadeState> FadeStates = [];

    /// <summary>
    /// Entities visible this frame.
    /// </summary>
    public readonly HashSet<EntityUid> SeenThisFrame = [];

    /// <summary>
    /// Whether FOV was enabled during the previous frame.
    /// </summary>
    public bool WasFovEnabled = true;

    public void Dispose()
    {
        foreach (var (uid, state) in FadeStates)
        {
            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(state.OriginalAlpha));
            _sprite.SetVisible((uid, sprite), true);
        }

        FadeStates.Clear();
        SeenThisFrame.Clear();
    }
}
