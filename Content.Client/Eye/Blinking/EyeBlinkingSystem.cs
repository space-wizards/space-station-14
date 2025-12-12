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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<EyeStateChangedEvent>(OnChangeEyeStateEvent);
        SubscribeNetworkEvent<BlinkEyeEvent>(OnBlinkEyeEvent);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EyeBlinkingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyelids, out var eyelids, false))
            return;

        // Maybe it's worth turning this into a field in a component that will be responsible for the sprite and State
        if (_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var eyes, false))
        {
            _sprite.LayerSetRsi(eyelids, eyes.RSI);
            _sprite.LayerSetRsiState(eyelids, eyes.State);
        }

        ResetBlink(ent);

        ChangeEyeState(ent, ent.Comp.EyesClosed);
    }

    private void OnChangeEyeStateEvent(EyeStateChangedEvent ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;

        ChangeEyeState((ent, blinkingComp), ev.EyesClosed);
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

    public void Blink(Entity<EyeBlinkingComponent> ent)
    {
        if (!ent.Owner.IsValid())
            return;

        ResetBlink(ent);

        if (ent.Comp.EyesClosed)
            return;

        if (ent.Comp.BlinkInProgress)
            return;

        ent.Comp.BlinkInProgress = true;
        var minDuration = ent.Comp.MinBlinkDuration;
        var maxDuration = ent.Comp.MaxBlinkDuration;
        var randomSeconds = minDuration + (_random.NextDouble() * (maxDuration - minDuration));
        ent.Comp.NextOpenEyeTime = _timing.CurTime + TimeSpan.FromSeconds(randomSeconds);

        ChangeEyeState(ent, true);
    }

    private void OpenEye(Entity<EyeBlinkingComponent> ent)
    {
        ent.Comp.BlinkInProgress = false;

        if (ent.Comp.EyesClosed)
            return;

        ChangeEyeState(ent, false);
    }

    public void ResetBlink(Entity<EyeBlinkingComponent> ent)
    {
        var minInterval = ent.Comp.MinBlinkInterval;
        var maxInterval = ent.Comp.MaxBlinkInterval;
        var randomSeconds = minInterval + (_random.NextDouble() * (maxInterval - minInterval));

        ent.Comp.NextBlinkingTime = _timing.CurTime + TimeSpan.FromSeconds(randomSeconds);
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
