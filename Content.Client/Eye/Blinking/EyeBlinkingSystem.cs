using Content.Shared.Body;
using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Eye.Blinking;

/// <inheritdoc/>
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IResourceCache _resCache = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeNetworkEvent]
    private void OnOpenEyes(OpenEyesEvent ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;

        var entComp = (ent, blinkingComp);
        ChangeEyesState(entComp, false);
        ResetBlink(entComp);
    }

    /// <summary>
    /// Initial eyelid initialization for all entities that should blink.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<EyeBlinkingComponent> ent, ref ComponentInit args)
    {
        if (TryComp<OrganComponent>(ent, out var organComp))
        {
            if (organComp.Body is { } body)
                InitEyeBlinking(ent, body);
        }
        else
        {
            InitEyeBlinking(ent, ent);
        }
    }

    /// <summary>
    /// Initializes eyelids following the <see cref="ApplyOrganMarkingsEvent">, when the entity receives skin color data for its organs
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<EyeBlinkingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Init)
            return;

        if (TryComp<OrganComponent>(ent, out var organComp))
        {
            if (organComp.Body is { } body)
                InitEyeBlinking(ent, body);
        }
        else
        {
            InitEyeBlinking(ent, ent.Owner);
        }
    }

    [SubscribeNetworkEvent]
    private void OnInitEyes(InitEyesEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        Logger.Info($"EyeBlinkingSystem: Received InitEyesEvent for entity {ev.NetEntity} with eyelid color {ev.EyelidsColor}");
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;
        blinkingComp.Init = false;

        if (TryComp<OrganComponent>(ent, out var organComp))
        {
            if (organComp.Body is { } body)
                InitEyeBlinking((ent, blinkingComp), body);
        }
        else
        {
            InitEyeBlinking((ent, blinkingComp), ent);
        }
    }

    /// <summary>
    /// Creates (or recreates) eyelid layers and initializes the client blinking component.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="body"></param>
    private void InitEyeBlinking(Entity<EyeBlinkingComponent> ent, EntityUid body)
    {
        Logger.Info($"EyeBlinkingSystem: Initializing eyelids for entity {ent.Owner} with body {body}");
        if (!TryComp<SpriteComponent>(body, out var sprite))
            return;

        if (!_sprite.TryGetLayer(body, HumanoidVisualLayers.Eyelids, out var eyelids, false))
            return;

        ent.Comp.Init = true;

        ent.Comp.Body = body;
        Logger.Info($"ent comp set body to {body} : {ent.Comp.Body}");

        InitEyelidsLayers(ent, body);

        // Initialize and randomize the blink timer.
        ResetBlink(ent);

        // Apply the initial eye state (open or closed).
        if (!(_appearance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var value) && value is bool eyeClosed))
        {
            ChangeEyesState(ent, false);
            return;
        }

        ChangeEyesState(ent, eyeClosed);
    }

    private void InitEyelidsLayers(Entity<EyeBlinkingComponent> ent, EntityUid body)
    {
        if (!TryComp<SpriteComponent>(body, out var comp))
            return;

        // Removes existing eyelid layers.
        for (var j = comp.AllLayers.Count() - 1; j >= 0; j--)
        {
            if (comp[j].RsiState.Name?.Contains("eyelid-") == true)
            {
                _sprite.RemoveLayer(body, j);
            }
        }

        // Clears eyelid states from the client component, if it already exists.
        ent.Comp.Eyelids.Clear();

        var rsiPath = ent.Comp.EyelidsSprite;
        if (rsiPath == null)
            return;

        if (!_resCache.TryGetResource<RSIResource>(rsiPath.Value, out var rsiRes))
        {
            Log.Error($"EyeBlinkingSystem: can't find RSI '{rsiPath}'");
            return;
        }

        // Checks if the eyelid layer is present.
        if (!_sprite.LayerMapTryGet((body, comp), HumanoidVisualLayers.Eyelids, out var targetLayer, false))
            return;

        var eyelidColor = Color.White;

        // If the entity has a specific eyelid color defined after organData init, use that color instead of the default white.
        if (ent.Comp.EyelidsColor != null)
        {
            eyelidColor = ent.Comp.EyelidsColor.Value;
        }

        var rsiCollection = rsiRes.RSI;
        int i = 0;

        // Creates a new layer for each eyelid state defined in the RSI.
        foreach (var state in rsiCollection)
        {
            var specifier = new SpriteSpecifier.Rsi(rsiPath.Value, state.StateId.Name!);
            var layerId = $"eyelids_extra_{state.StateId}";

            if (!_sprite.LayerMapTryGet((body, comp), layerId, out var existingLayer, false))
            {
                var layer = _sprite.AddLayer((body, comp), specifier, targetLayer + i + 1);
                _sprite.LayerMapSet((body, comp), layerId, layer);
            }

            _sprite.LayerSetSprite((body, comp), layerId, specifier);
            _sprite.LayerSetColor((body, comp), layerId, eyelidColor);
            _sprite.LayerSetVisible((body, comp), layerId, false);

            ent.Comp.Eyelids.Add(new EyelidState(layerId));

            i++;
        }
    }

    /// <summary>
    /// Handles the appearance change event for entities with the <see cref="EyeBlinkingComponent"/>.
    /// This method checks if the eye state has changed (open or closed) and updates the eyelid layers accordingly.
    /// If the eyes are closed or if a blink is not in progress, it changes the eye state immediately.
    /// Otherwise, it allows the blink to complete before changing the state.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<EyeBlinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var value) || !(value is bool eyeClosed))
            return;

        if ((eyeClosed == false && ent.Comp.BlinkInProgress == false) ||
            eyeClosed)
        {
            ChangeEyesState(ent, eyeClosed);
            return;
        }
    }

    /// <summary>
    /// Handles the blink eye event received from the network.
    /// This method retrieves the entity associated with the event and checks if it has a valid <see cref="EyeBlinkingComponent"/>.
    /// If the entity is valid and has the component, it initiates a blink action for that entity.
    /// </summary>
    [SubscribeNetworkEvent]
    private void OnBlinkEyes(BlinkEyeEvent ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (!ent.IsValid() || !TryComp<EyeBlinkingComponent>(ent, out var blinkingComp))
            return;

        Blink((ent, blinkingComp));
    }

    /// <summary>
    /// Changes the eye state (open or closed) for the specified entity.
    /// This method updates the visibility of the eyelid layers based on the provided eye state.
    /// If the entity does not have a valid <see cref="SpriteComponent"/> or if the eyelid layer is not found,
    /// the method exits without making any changes.
    /// </summary>
    /// <param name="eyeClosed">Value close eye if true, and open if false</param>
    private void ChangeEyesState(Entity<EyeBlinkingComponent> ent, bool eyeClosed)
    {
        if (!TryComp<SpriteComponent>(ent.Comp.Body, out var sprite))
            return;

        if (!_sprite.TryGetLayer(ent.Comp.Body.Value, HumanoidVisualLayers.Eyelids, out var layer, false))
            return;

        foreach (var eyelidState in ent.Comp.Eyelids)
            ChangeEyeState((ent.Comp.Body.Value, sprite), eyelidState, eyeClosed);
    }

    /// <summary>
    /// Initiates a blink action for the specified entity if its eyes are currently open and no blink is already in
    /// progress.
    /// </summary>
    /// <remarks>If a blink is already in progress or the entity's eyes are closed, this method has no effect.
    /// The blink duration is determined randomly within the component's configured minimum and maximum blink
    /// durations.</remarks>
    /// <param name="ent">The entity containing the <see cref="EyeBlinkingComponent"/> to blink.
    /// The entity's owner must be valid, and its eyes must not already be closed.</param>
    public void Blink(Entity<EyeBlinkingComponent> ent)
    {
        if (!ent.Owner.IsValid())
            return;

        if (ent.Comp.Enabled == false)
            return;

        // Checks if a blink is still in progress to avoid a "frozen eyes" effect caused by emote spamming. A blink must complete fully before the next one can start.
        if (ent.Comp.BlinkInProgress)
            return;

        if (ent.Comp.Eyelids.Count == 0)
            return;

        // Marks the blink as in progress to prevent overlapping blinks.
        ent.Comp.BlinkInProgress = true;

        var curTime = _timing.CurTime;
        var maxOpenTime = curTime;

        // Randomly determines the duration of the blink within the configured range.
        var minDuration = ent.Comp.MinBlinkDuration;
        var maxDuration = ent.Comp.MaxBlinkDuration;
        var blinkDuration = minDuration + (_random.NextDouble() * (maxDuration - minDuration));

        // Retrieves the eyelid states from the client component to schedule the blink timings for each eyelid.
        var eyelidStates = ent.Comp.Eyelids;

        // Retrieves the maximum asynchronous blink and open blink durations, considering any status effects that may modify these values.
        var maxAsyncBlink = ent.Comp.MaxAsyncBlink;
        var maxAsyncOpenBlink = ent.Comp.MaxAsyncOpenBlink;

        // Checks for any status effects that may modify the maximum asynchronous blink and open blink durations.
        if (_statusEffects.TryEffectsWithComp<BlinkDyspraxiaStatusEffectComponent>(ent.Owner, out var effects))
        {
            foreach (var effect in effects)
            {
                maxAsyncBlink = maxAsyncBlink.Add(effect.Comp1.MaxAsyncBlink);
                maxAsyncOpenBlink = maxAsyncOpenBlink.Add(effect.Comp1.MaxAsyncOpenBlink);
            }
        }

        // Schedules the close and open times for each eyelid state.
        foreach (var eyelidState in eyelidStates)
        {
            // Schedules the close time for the eyelid, adding a random offset to create asynchronous blinking. If maxAsyncBlink is zero, the eyelids will close simultaneously.
            var scheduleCloseTime = curTime + _random.NextDouble() * maxAsyncBlink + ent.Comp.PausedOffset;

            // Schedules the open time for the eyelid, adding a random offset to create asynchronous opening. If maxAsyncOpenBlink is zero, the eyelids will open simultaneously.
            // calculates the open time based on the close time, blink duration, and a random offset for asynchronous opening.
            var scheduleOpenTime = scheduleCloseTime + blinkDuration + _random.NextDouble() * maxAsyncOpenBlink + ent.Comp.PausedOffset;

            // Updates the eyelid state with the scheduled close and open times.
            eyelidState.ScheduledCloseTime = scheduleCloseTime;
            eyelidState.ScheduledOpenTime = scheduleOpenTime;

            // Updates the maximum open time to ensure that the next blink is scheduled after all eyelids have completed their opening.
            if (scheduleOpenTime > maxOpenTime)
                maxOpenTime = scheduleOpenTime;
        }

        ent.Comp.NextOpenEyesTime = maxOpenTime;

        ResetBlink(ent);
    }

    private void ChangeEyeState(Entity<SpriteComponent?> ent, EyelidState state, bool eyeClosed)
    {
        var layer = state.LayerKey;
        state.IsClosed = eyeClosed;
        state.IsCompleteBlink = !eyeClosed;
        _sprite.LayerSetVisible(ent, layer, eyeClosed);
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
        var randomBlinkInterval = minInterval + (_random.NextDouble() * (maxInterval - minInterval));

        // Schedules the next blink time based on the last open eye time and the randomly determined interval.
        ent.Comp.NextBlinkingTime = ent.Comp.NextOpenEyesTime + randomBlinkInterval;
    }

    /// <summary>
    /// Updates blinking logic for all entities with active blinking components.
    /// Handles the timing for closing/opening eyelids during an active blink and schedules the next random blink.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EyeBlinkingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;

            if (!TryComp<SpriteComponent>(comp.Body, out var spriteComp))
                continue;

            // If a blink is currently in progress, check the scheduled times for each eyelid and update their states accordingly.
            if (comp.BlinkInProgress)
            {
                foreach (var eyelidState in comp.Eyelids)
                {
                    // If the eyelid is not closed and the current time has reached or passed the scheduled close time, close the eyelid.
                    if (!eyelidState.IsClosed && curTime >= eyelidState.ScheduledCloseTime && eyelidState.IsCompleteBlink == false)
                    {
                        ChangeEyeState((comp.Body.Value, spriteComp), eyelidState, true);
                    }
                    // If the eyelid is closed and the current time has reached or passed the scheduled open time, open the eyelid.
                    else if (eyelidState.IsClosed && curTime >= eyelidState.ScheduledOpenTime)
                    {
                        ChangeEyeState((comp.Body.Value, spriteComp), eyelidState, false);
                    }
                }

                // If all eyelids have completed their blink (i.e., they are all open), reset the blink state and schedule the next blink.
                if (comp.Eyelids.All(e => e.IsCompleteBlink))
                {
                    comp.Eyelids.ForEach(e => e.IsCompleteBlink = false);
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
