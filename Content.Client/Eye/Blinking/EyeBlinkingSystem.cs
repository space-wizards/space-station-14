using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;
using System.ComponentModel.Design;
using static Content.Shared.Fax.AdminFaxEuiMsg;

namespace Content.Client.Eye.Blinking;
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ITimerManager _timer = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ChangeEyeStateEvent>(OnChangeEyeStateEvent);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EyeBlinkingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        ChangeEyeState(ent, ent.Comp.EyesClosed);
    }

    private void ChangeEyeState(Entity<EyeBlinkingComponent> ent, bool eyeClsoed)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;
        if (!_sprite.LayerMapTryGet(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
        var blinkColor = new Color(
            humanoid.SkinColor.R * blinkFade,
            humanoid.SkinColor.G * blinkFade,
            humanoid.SkinColor.B * blinkFade);
        var eyeColor = humanoid.EyeColor;
        _sprite.LayerSetColor((ent.Owner, sprite), HumanoidVisualLayers.Eyes, eyeClsoed ? blinkColor : eyeColor);
    }

    private void OnChangeEyeStateEvent(ChangeEyeStateEvent ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;

        ChangeEyeState((ent, blinkingComp), ev.EyesClosed);
    }

    public override void BlindnessChangedEventHanlder(Entity<EyeBlinkingComponent> ent, ref BlindnessChangedEvent args)
    {
        base.BlindnessChangedEventHanlder(ent, ref args);
        if (!ent.Owner.IsValid())
            return;

        ChangeEyeState(ent, args.Blind);
    }

    public override void Blink(Entity<EyeBlinkingComponent> ent)
    {
        if (!ent.Owner.IsValid())
            return;

        base.Blink(ent);

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
}
