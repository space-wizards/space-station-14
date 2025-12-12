using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Eye.Blinking;

public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ITimerManager _timer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeBlinkingComponent, ChangeEyeStateEvent>(OnChangeEyeStateEvent);
        SubscribeLocalEvent<EyeBlinkingComponent, BlinkEyeEvent>(OnBlinkEyeEvent);
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

        ChangeEyeState(ent, ent.Comp.EyesClosed);
    }

    private void OnChangeEyeStateEvent(Entity<EyeBlinkingComponent> ent, ref ChangeEyeStateEvent args)
    {
        ChangeEyeState(ent, args.EyesClosed);
    }

    private void OnBlinkEyeEvent(Entity<EyeBlinkingComponent> ent, ref BlinkEyeEvent args)
    {
        Blink(ent);
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

    public override void BlindnessChangedEventHanlder(Entity<EyeBlinkingComponent> ent, ref BlindnessChangedEvent args)
    {
        base.BlindnessChangedEventHanlder(ent, ref args);
        if (!ent.Owner.IsValid())
            return;

        ChangeEyeState(ent, args.Blind);
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

        ChangeEyeState(ent, true);

        Timer timer = new Timer((int)ent.Comp.BlinkDuration.TotalMilliseconds, false, () => OpenEye(ent));

        _timer.AddTimer(timer);
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
        ent.Comp.NextBlinkingTime = _timing.CurTime + ent.Comp.BlinkInterval + ent.Comp.BlinkDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EyeBlinkingComponent>();

        while (query.MoveNext(out var uid, out var comp) && comp.Enabled)
        {
            if (comp.NextBlinkingTime > curTime)
                continue;

            Blink((uid, comp));
        }
    }
}
