using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Eye.Blinking;

public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _apperance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeBlinkingComponent, AppearanceChangeEvent>(OnApperanceChangeEventHandler);
        SubscribeNetworkEvent<BlinkEyeEvent>(OnBlinkEyeEvent);
        SubscribeLocalEvent<EyeBlinkingComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<EyeBlinkingComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        // Check if the entity has an Eyelids layer. If not, we can't do anything visually.
        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyelids, out var eyelids, false))
            return;

        // Attempt to sync the eyelids' RSI and state with the Eyes layer for a consistent look.
        // NOTE: This logic needs to be expanded to support other mobs that use randomized colors or sprites (e.g., Scurrets).
        // - Mice and other simple mobs work out-of-the-box. They only require an eyelid sprite and a set color of #ffffff so it isn't overridden.
        // - Scurrets are currently problematic due to their use of RandomSprite; we need a way to handle this after color initialization.
        // Maybe it's worth turning this into a field in the component that determines the sprite and State.
        if (_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var eyes, false))
        {
            _sprite.LayerSetRsi(eyelids, eyes.RSI);
            _sprite.LayerSetRsiState(eyelids, eyes.State);
        }

        // Initialize and randomize the blink timer.
        ResetBlink(ent);

        // Apply the initial eye state (open or closed).
        if (!(_apperance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var value) && value is bool eyeClosed))
        {
            ChangeEyeState(ent, false);
            return;
        }

        ChangeEyeState(ent, eyeClosed);
    }

    private void OnApperanceChangeEventHandler(Entity<EyeBlinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!(args.AppearanceData.TryGetValue(EyeBlinkingVisuals.EyesClosed, out var value) && value is bool eyeClosed))
            return;

        if ((eyeClosed == false && ent.Comp.BlinkInProgress == false) ||
            eyeClosed)
        {
            ChangeEyeState(ent, eyeClosed);
            return;
        }
    }


    private void OnBlinkEyeEvent(BlinkEyeEvent ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;

        Blink((ent, blinkingComp));
    }

    private void ChangeEyeState(Entity<EyeBlinkingComponent> ent, bool eyeClsoed)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;
        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyelids, out var layer, false))
            return;

        var blinkColor = Color.Transparent;

        if (ent.Comp.EyelidsColor == null && TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
        {
            var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
            blinkColor = new Color(
                humanoid.SkinColor.R * blinkFade,
                humanoid.SkinColor.G * blinkFade,
                humanoid.SkinColor.B * blinkFade);
        }
        else if (ent.Comp.EyelidsColor != null)
        {
            blinkColor = ent.Comp.EyelidsColor.Value;
        }

        _sprite.LayerSetColor(layer, eyeClsoed ? blinkColor : Color.Transparent);
    }

    /// <summary>
    /// Initiates a blink action for the specified entity if its eyes are currently open and no blink is already in
    /// progress.
    /// </summary>
    /// <remarks>If a blink is already in progress or the entity's eyes are closed, this method has no effect.
    /// The blink duration is determined randomly within the component's configured minimum and maximum blink
    /// durations.</remarks>
    /// <param name="ent">The entity containing the <see cref="EyeBlinkingComponent"/> to blink. The entity's owner must be valid, and its
    /// eyes must not already be closed.</param>
    public void Blink(Entity<EyeBlinkingComponent> ent)
    {
        if (!ent.Owner.IsValid())
            return;

        if (_apperance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var value) && value is bool eyeClosed && eyeClosed)
                return;

        if (ent.Comp.BlinkInProgress)
            return;

        ResetBlink(ent);

        ent.Comp.BlinkInProgress = true;
        var minDuration = ent.Comp.MinBlinkDuration;
        var maxDuration = ent.Comp.MaxBlinkDuration;
        var randomSeconds = minDuration + (_random.NextDouble() * (maxDuration - minDuration));
        ent.Comp.NextOpenEyeTime = _timing.CurTime + randomSeconds;

        ChangeEyeState(ent, true);
    }

    private void OpenEye(Entity<EyeBlinkingComponent> ent)
    {
        ent.Comp.BlinkInProgress = false;

        if (_apperance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var value) && value is bool eyeClosed && eyeClosed)
            return;

        ChangeEyeState(ent, false);
    }

    /// <summary>
    /// Resets the blink timer for the specified entity, scheduling the next blink within the entity's configured
    /// interval range.
    /// </summary>
    /// <remarks>The next blink time is set to a random value between the minimum and maximum blink intervals,
    /// starting from the current time.</remarks>
    /// <param name="ent">The entity whose blink timer is to be reset. The entity must have a valid <see cref="EyeBlinkingComponent"/>
    /// with defined minimum and maximum blink intervals.</param>
    public void ResetBlink(Entity<EyeBlinkingComponent> ent)
    {
        var minInterval = ent.Comp.MinBlinkInterval;
        var maxInterval = ent.Comp.MaxBlinkInterval;
        var randomSeconds = minInterval + (_random.NextDouble() * (maxInterval - minInterval));

        ent.Comp.NextBlinkingTime = _timing.CurTime + randomSeconds;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EyeBlinkingComponent>();

        while (query.MoveNext(out var uid, out var comp) && comp.Enabled)
        {
            if (comp.BlinkInProgress)
            {
                if (curTime >= comp.NextOpenEyeTime)
                {
                    OpenEye((uid, comp));
                }
                continue;
            }
            if (comp.NextBlinkingTime > curTime)
                continue;

            Blink((uid, comp));
        }
    }
}
