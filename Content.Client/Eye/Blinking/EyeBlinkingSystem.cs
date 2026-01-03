using Content.Shared.Chat.Prototypes;
using Content.Shared.Cloning.Events;
using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Eye.Blinking;

public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _apperance = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeBlinkingComponent, AppearanceChangeEvent>(OnApperanceChangeEventHandler);
        SubscribeNetworkEvent<BlinkEyeEvent>(OnBlinkEyeEvent);
        SubscribeNetworkEvent<UpdateEyelidsAfterCloningEvent>(OnUpdateEyelidsAfterCloningEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentInit>(OnComponentInit);
    }

    private void OnUpdateEyelidsAfterCloningEventHandler(UpdateEyelidsAfterCloningEvent ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;

        Blink((ent, blinkingComp));
    }

    private void OnComponentInit(Entity<EyeBlinkingComponent> ent, ref ComponentInit args)
    {
        InitEyeBlinking(ent);
    }

    private void InitEyeBlinking(Entity<EyeBlinkingComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var comp))
            return;

        // Check if the entity has an Eyelids layer. If not, we can't do anything visually.
        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyelids, out var eyelids, false))
            return;

        var clientComp = EntityManager.EnsureComponent<EyeBlinkingClientComponent>(ent.Owner);

        InitEyelidsLayers(ent);

        var allEyelids = comp.AllLayers.Where(layer => layer.RsiState.Name?.Contains("eyelid-") == true);
        foreach (var layer in allEyelids)
        {
            clientComp.Eyelids.Add(new EyelidState(layer));
        }

        // Attempt to sync the eyelids' RSI and state with the Eyes layer for a consistent look.
        // NOTE: This logic needs to be expanded to support other mobs that use randomized colors or sprites (e.g., Scurrets).
        // - Mice and other simple mobs work out-of-the-box. They only require an eyelid sprite and a set color of #ffffff so it isn't overridden.
        // - Scurrets are currently problematic due to their use of RandomSprite; we need a way to handle this after color initialization.

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

    private void InitEyelidsLayers(Entity<EyeBlinkingComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComp))
            return;

        var rsiPath = ent.Comp.EyelidsSprite;
        if (rsiPath == null)
            return;

        if (!_resCache.TryGetResource<RSIResource>(rsiPath.Value, out var rsiRes))
        {
            Log.Error($"EyeBlinkingSystem: can't find RSI '{rsiPath}'");
            return;
        }
        if (!_sprite.LayerMapTryGet((ent.Owner, spriteComp), HumanoidVisualLayers.Eyelids, out var targetLayer, false))
        {
            return;
        }

        var rsiCollection = rsiRes.RSI;
        int i = 0;

        foreach (var state in rsiCollection)
        {
            var specifier = new SpriteSpecifier.Rsi(rsiPath.Value, state.StateId.Name!);
            var layerId = $"eyelids_extra_{state.StateId}";

            if (!_sprite.LayerMapTryGet((ent.Owner, spriteComp), layerId, out var existingLayer, false))
            {
                var layer = _sprite.AddLayer((ent.Owner, spriteComp), specifier, targetLayer + i + 1);
                _sprite.LayerMapSet((ent.Owner, spriteComp), layerId, layer);
                _sprite.LayerSetSprite((ent.Owner, spriteComp), layerId, specifier);
            }
            else
            {
                _sprite.LayerSetSprite((ent.Owner, spriteComp), layerId, specifier);
            }
            _sprite.LayerSetColor((ent.Owner, spriteComp), layerId, Color.Transparent);
            i++;
        }
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

        foreach (var eyelidState in EntityManager.GetComponent<EyeBlinkingClientComponent>(ent.Owner).Eyelids)
        {
            if (eyeClsoed)
            {
                CloseEye(ent, eyelidState);
            }
            else
            {
                OpenEye(ent, eyelidState);
            }
        }
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

        if (ent.Comp.Enabled == false)
            return;

        if (_apperance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var value) && value is bool eyeClosed && eyeClosed)
            return;

        if (ent.Comp.BlinkInProgress)
            return;

        if (TryComp<EyeBlinkingClientComponent>(ent.Owner, out var clientComp))
        {
            if (clientComp.Eyelids.Count == 0)
            {
                return;
            }
        }
        else
        {
            return;
        }

        ent.Comp.BlinkInProgress = true;

        var curTime = _timing.CurTime;
        var maxOpenTime = curTime;

        var minDuration = ent.Comp.MinBlinkDuration;
        var maxDuration = ent.Comp.MaxBlinkDuration;
        var blinkDuration = minDuration + (_random.NextDouble() * (maxDuration - minDuration));

        var eyelidStates = clientComp.Eyelids;

        foreach (var eyelidState in eyelidStates)
        {
            var sheduleCloseTime = curTime + _random.NextDouble() * ent.Comp.MaxAsyncBlink + clientComp.PausedOffset;
            var sheduleOpenTime = sheduleCloseTime + blinkDuration + _random.NextDouble() * ent.Comp.MaxAsyncOpenBlink + clientComp.PausedOffset;

            eyelidState.ScheduledCloseTime = sheduleCloseTime;
            eyelidState.ScheduledOpenTime = sheduleOpenTime;

            if (sheduleOpenTime > maxOpenTime) maxOpenTime = sheduleOpenTime;
        }

        ent.Comp.NextOpenEyesTime = maxOpenTime;

        ResetBlink(ent);
    }

    private void CloseEye(Entity<EyeBlinkingComponent> ent, EyelidState state)
    {
        var layer = state.Layer;
        state.IsClosed = true;
        state.IsCompleteBlink = false;

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

        layer.Color = blinkColor;
    }

    private void OpenEye(Entity<EyeBlinkingComponent> ent, EyelidState state)
    {
        var layer = state.Layer;
        state.IsClosed = false;
        state.IsCompleteBlink = true;
        layer.Color = Color.Transparent;
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

        ent.Comp.NextBlinkingTime = ent.Comp.NextOpenEyesTime + randomSeconds;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EyeBlinkingComponent, EyeBlinkingClientComponent>();

        while (query.MoveNext(out var uid, out var comp, out var clientComp) && comp.Enabled)
        {
            if (comp.BlinkInProgress)
            {
                foreach (var eyelidState in clientComp.Eyelids)
                {
                    //Logger.Info($"Close Eye for entity = {!eyelidState.IsClosed && curTime >= eyelidState.ScheduledCloseTime && curTime < eyelidState.ScheduledOpenTime}; Open eye for entity: {eyelidState.IsClosed && curTime >= eyelidState.ScheduledOpenTime}");
                    if (!eyelidState.IsClosed && curTime >= eyelidState.ScheduledCloseTime && eyelidState.IsCompleteBlink == false)
                    {
                        CloseEye((uid, comp), eyelidState);
                    }
                    else if (eyelidState.IsClosed && curTime >= eyelidState.ScheduledOpenTime)
                    {
                        OpenEye((uid, comp), eyelidState);
                    }
                }
                if (clientComp.Eyelids.All(e => e.IsCompleteBlink))
                {
                    clientComp.Eyelids.ForEach(e => e.IsCompleteBlink = false);
                    comp.BlinkInProgress = false;
                    ResetBlink((uid, comp));
                    continue;
                }
            }

            if (comp.NextBlinkingTime > curTime)
                continue;

            Blink((uid, comp));
        }
    }
}
