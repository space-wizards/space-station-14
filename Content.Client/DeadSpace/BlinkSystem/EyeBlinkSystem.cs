// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Bed.Sleep;
using Content.Shared.Blink;

namespace Content.Client.BlinkSystem;

public sealed class EyeBlinkSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    private const string BlinkLayerKey = "humanoid_blink_layer";
    private const float UpdateInterval = 0.1f;

    private readonly ResPath _rsiPath = new("/Textures/_DeadSpace/Effects/blink.rsi");

    private readonly Dictionary<EntityUid, BlinkData> _blinkData = new();
    private readonly List<EntityUid> _staleBlinkData = new();
    private float _updateAccumulator;

    private readonly string[] _skipMarkingKeys = 
    {
        "Malstrem-malstrem",
        "Malstrem2-malstrem2",
        "Terminator-terminator",
        "Beholder-beholder",
        "GauzeLefteyePatch-gauze_lefteye_2",
        "GauzeRighteyePatch-gauze_righteye_2",
        "GauzeLefteyePad-gauze_lefteye_1",
        "GauzeRighteyePad-gauze_righteye_1",
        "GauzeBlindfold-gauze_blindfold"
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkComponent, ComponentStartup>(OnBlinkStartup);
        SubscribeLocalEvent<BlinkComponent, ComponentShutdown>(OnBlinkShutdown);
        SubscribeLocalEvent<BlinkComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SleepingComponent, ComponentStartup>(OnSleepStartup);
        SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnSleepShutdown);
    }

    private void OnBlinkStartup(EntityUid uid, BlinkComponent component, ComponentStartup args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var appearance) ||
            !TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryEnsureBlinkLayer(uid, sprite, appearance))
            return;

        if (!_blinkData.TryAdd(uid, new BlinkData()))
            return;

        var data = _blinkData[uid];
        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState is MobState.Dead or MobState.Critical)
        {
            return;
        }

        if (HasComp<SleepingComponent>(uid))
        {
            data.IsClosed = true;
            SetBlinkVisible(uid, !HasSkipMarkings(sprite), sprite, appearance);
            return;
        }

        ScheduleBlink(data, NextBlinkDelay());
    }

    private bool TryEnsureBlinkLayer(EntityUid uid, SpriteComponent sprite, HumanoidAppearanceComponent appearance)
    {
        if (sprite.LayerMapTryGet(BlinkLayerKey, out _))
            return true;

        var meta = MetaData(uid);
        var protoId = meta.EntityPrototype?.ID;
        if (protoId == null)
            return false;

        if (protoId.Contains("MobDiona") || protoId.Contains("MobXenomorph") ||
            protoId.Contains("MobIPC") || protoId.Contains("MobGingerbread") ||
            protoId.Contains("MobSkeleton") || protoId.Contains("MobSlimePerson"))
            return false;

        string state = "eye_blink";
        if (protoId.Contains("MobVox")) state = "eye_blink_vox";
        else if (protoId.Contains("MobArachnid")) state = "eye_blink_arachnid";
        else if (protoId.Contains("MobMoth")) state = "eye_blink_moth";
        else if (protoId.Contains("MobKobolt") || protoId.Contains("MobReptilian")) state = "eye_blink_reptilian";

        if (!sprite.LayerMapTryGet(HumanoidVisualLayers.Eyes, out var eyeLayerIndex))
            return false;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(_rsiPath, state), eyeLayerIndex + 2);
        sprite.LayerMapSet(BlinkLayerKey, layer);
        sprite.LayerSetVisible(layer, false);
        sprite.LayerSetColor(layer, appearance.SkinColor);

        return true;
    }

    private bool HasSkipMarkings(SpriteComponent sprite)
    {
        foreach (var key in _skipMarkingKeys)
        {
            if (sprite.LayerMapTryGet(key, out _))
                return true;
        }
        return false;
    }

    private void OnBlinkShutdown(EntityUid uid, BlinkComponent component, ComponentShutdown args)
    {
        _blinkData.Remove(uid);

        if (TryComp<SpriteComponent>(uid, out var sprite) && sprite.LayerMapTryGet(BlinkLayerKey, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnMobStateChanged(Entity<BlinkComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_blinkData.TryGetValue(ent, out var data))
            return;

        if (args.NewMobState is MobState.Dead or MobState.Critical)
        {
            data.IsClosed = false;
            SetBlinkVisible(ent, false);
            return;
        }

        if (args.OldMobState is not (MobState.Dead or MobState.Critical))
            return;

        if (HasComp<SleepingComponent>(ent))
        {
            data.IsClosed = true;
            if (TryComp<SpriteComponent>(ent, out var sprite))
                SetBlinkVisible(ent, !HasSkipMarkings(sprite), sprite);
            return;
        }

        ScheduleBlink(data, NextBlinkDelay());
    }

    private void OnSleepStartup(EntityUid uid, SleepingComponent component, ComponentStartup args)
    {
        if (!_blinkData.TryGetValue(uid, out var data))
            return;

        data.IsClosed = true;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState is MobState.Dead or MobState.Critical)
        {
            SetBlinkVisible(uid, false);
            return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite) || HasSkipMarkings(sprite))
        {
            SetBlinkVisible(uid, false, sprite);
            return;
        }

        SetBlinkVisible(uid, true, sprite);
    }

    private void OnSleepShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
    {
        if (!_blinkData.TryGetValue(uid, out var data))
            return;

        data.IsClosed = false;
        SetBlinkVisible(uid, false);

        if (!TryComp<MobStateComponent>(uid, out var mobState) ||
            mobState.CurrentState is not (MobState.Dead or MobState.Critical))
        {
            ScheduleBlink(data, NextBlinkDelay());
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        var elapsed = _updateAccumulator;
        _updateAccumulator %= UpdateInterval;
        _staleBlinkData.Clear();

        foreach (var (uid, data) in _blinkData)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite) ||
                !TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            {
                _staleBlinkData.Add(uid);
                continue;
            }

            if (!sprite.LayerMapTryGet(BlinkLayerKey, out var layerIndex))
            {
                if (!TryEnsureBlinkLayer(uid, sprite, appearance) ||
                    !sprite.LayerMapTryGet(BlinkLayerKey, out layerIndex))
                {
                    continue;
                }
            }

            if (HasSkipMarkings(sprite))
            {
                sprite.LayerSetVisible(layerIndex, false);
                continue;
            }

            if (TryComp<MobStateComponent>(uid, out var mobState) &&
                mobState.CurrentState is MobState.Dead or MobState.Critical)
            {
                data.IsClosed = false;
                sprite.LayerSetVisible(layerIndex, false);
                continue;
            }

            if (HasComp<SleepingComponent>(uid))
            {
                data.IsClosed = true;
                sprite.LayerSetColor(layerIndex, appearance.SkinColor);
                sprite.LayerSetVisible(layerIndex, true);
                continue;
            }

            data.TimeLeft -= elapsed;
            if (data.TimeLeft > 0f)
                continue;

            if (data.IsClosed)
            {
                data.IsClosed = false;
                sprite.LayerSetVisible(layerIndex, false);
                ScheduleBlink(data, NextBlinkDelay());
                continue;
            }

            data.IsClosed = true;
            data.TimeLeft = 1.5f;
            sprite.LayerSetColor(layerIndex, appearance.SkinColor);
            sprite.LayerSetVisible(layerIndex, true);
        }

        foreach (var uid in _staleBlinkData)
        {
            _blinkData.Remove(uid);
        }
    }

    private void SetBlinkVisible(EntityUid uid, bool visible, SpriteComponent? sprite = null,
        HumanoidAppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref sprite, false) ||
            !sprite.LayerMapTryGet(BlinkLayerKey, out var layerIndex))
            return;

        if (visible && Resolve(uid, ref appearance, false))
            sprite.LayerSetColor(layerIndex, appearance.SkinColor);

        sprite.LayerSetVisible(layerIndex, visible);
    }

    private float NextBlinkDelay()
    {
        return _random.NextFloat(30f, 80f);
    }

    private static void ScheduleBlink(BlinkData data, float delay)
    {
        data.TimeLeft = delay;
    }

    private sealed class BlinkData
    {
        public float TimeLeft;
        public bool IsClosed;
    }
}
