using Content.Client.Items.Systems;
using Content.Client.Trigger.Components;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Trigger;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Trigger.Systems;

public sealed partial class TimerTriggerVisualizerSystem : VisualizerSystem<TimerTriggerVisualsComponent>
{
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimerTriggerVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TimerTriggerVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });

    }

    private void OnComponentInit(Entity<TimerTriggerVisualsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PrimingAnimation = new Animation
        {
            Length = TimeSpan.MaxValue,
            AnimationTracks = {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = TriggerVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.PrimingSprite, 0f) }
                }
            },
        };

        if (ent.Comp.PrimingSound != null)
        {
            ent.Comp.PrimingAnimation.AnimationTracks.Add(
                new AnimationTrackPlaySound()
                {
                    KeyFrames = { new AnimationTrackPlaySound.KeyFrame(_audioSystem.ResolveSound(ent.Comp.PrimingSound), 0) }
                }
            );
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, TimerTriggerVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null
            && TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
        {
            if (!AppearanceSystem.TryGetData<TriggerVisualState>(uid, TriggerVisuals.VisualState, out var state, args.Component))
                state = TriggerVisualState.Unprimed;

            switch (state)
            {
                case TriggerVisualState.Primed:
                    if (!AnimationSystem.HasRunningAnimation(uid, animPlayer, TimerTriggerVisualsComponent.AnimationKey))
                        AnimationSystem.Play((uid, animPlayer), comp.PrimingAnimation, TimerTriggerVisualsComponent.AnimationKey);
                    break;
                case TriggerVisualState.Unprimed:
                    SpriteSystem.LayerSetRsiState((uid, args.Sprite), TriggerVisualLayers.Base, comp.UnprimedSprite);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // in-hand visuals
        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, TimerTriggerVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (component.InHandPrimedName == null)
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (!AppearanceSystem.TryGetData<TriggerVisualState>(uid, TriggerVisuals.VisualState, out var state, appearance))
            state = TriggerVisualState.Unprimed;

        var layer = new PrototypeLayerData();

        // Selects inhand sprites to load based on primed state, e.g. inhand-right-unprimed or inhand-right-primed
        var heldPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand";
        var key = heldPrefix + args.Location.ToString().ToLowerInvariant();
        if (state == TriggerVisualState.Primed)
            key += component.InHandPrimedName;
        else if (component.InHandUnprimedName != null)
            key += component.InHandUnprimedName; //some have unique unprimed sprites, e.g. smoke grenades
        else
            return; // using default in-hand sprite; no need to duplicate layer

        layer.State = key;
        args.Layers.Add((key, layer));
    }
}

public enum TriggerVisualLayers : byte
{
    Base
}
