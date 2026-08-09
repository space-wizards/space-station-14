using System.Linq;
using Content.Client.DisplacementMap;
using Content.Shared.Body;
using Content.Shared.DisplacementMap;
using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Eye.Blinking;

/// <inheritdoc/>
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IResourceCache _resCache = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    /// <summary>
    /// A prefix added to the eyelid layers added to body sprites.
    /// </summary>
    public const string LayerPrefix = "eyelid_extra";

    #region Event Handlers
    /// <summary>
    /// Initial eyelid initialization for all entities that should blink.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<EyeBlinkingComponent> ent, ref ComponentStartup _)
    {
        InitEyeBlinking(ent, GetActiveEntity(ent));
    }

    /// <summary>
    /// Initializes eyelids following the <see cref="ApplyOrganMarkingsEvent">, when the entity receives skin color data for its organs
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<EyeBlinkingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.LastEyelidsColor != ent.Comp.EyelidsColor)
        {
            ent.Comp.LastEyelidsColor = ent.Comp.EyelidsColor;
            if (ent.Comp.Body is { } body)
                UpdateEyelidsColor(ent, body);
        }

        if (ent.Comp.Status != ent.Comp.LastStatus)
        {
            StatusChanged(ent, ent.Comp.LastStatus);

            ent.Comp.LastStatus = ent.Comp.Status;
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

        if (!ent.IsValid() || !EyeBlinkingQuery.TryComp(ent, out EyeBlinkingComponent? blinkingComp))
            return;

        Blink((ent, blinkingComp));
    }
    #endregion Event Handlers

    #region Public API
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

        if (ent.Comp.Status != BlinkStatus.Normal)
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
        var blinkDuration = _random.Next(minDuration, maxDuration);

        // Retrieves the eyelid states from the client component to schedule the blink timings for each eyelid.
        var eyelidStates = ent.Comp.Eyelids;

        // Retrieves the maximum asynchronous blink and open blink durations, considering any status effects that may modify these values.
        var maxAsyncBlink = ent.Comp.MaxAsyncBlink;
        var maxAsyncOpenBlink = ent.Comp.MaxAsyncOpenBlink;

        // Checks for any status effects that may modify the maximum asynchronous blink and open blink durations.
        if (_statusEffects.TryEffectsWithComp<BlinkDyspraxiaStatusEffectComponent>(ent.Comp.Body, out var effects))
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
            var scheduleCloseTime = curTime + _random.Next(maxAsyncBlink);

            // Schedules the open time for the eyelid, adding a random offset to create asynchronous opening. If maxAsyncOpenBlink is zero, the eyelids will open simultaneously.
            // calculates the open time based on the close time, blink duration, and a random offset for asynchronous opening.
            var scheduleOpenTime = scheduleCloseTime + blinkDuration + _random.Next(maxAsyncOpenBlink);

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
        var randomBlinkInterval = _random.Next(minInterval, maxInterval);

        // Schedules the next blink time based on the last open eye time and the randomly determined interval.
        ent.Comp.NextBlinkingTime = ent.Comp.NextOpenEyesTime + randomBlinkInterval;
    }
    #endregion Public API

    #region Internal
    /// <summary>
    /// Updates a given eyelid state if it exists.
    /// </summary>
    private void ChangeEyeState(Entity<SpriteComponent?> ent, EyelidState state, bool eyeClosed)
    {
        var layer = state.LayerKey;
        if (!_sprite.LayerMapTryGet(ent, layer, out var layerIndex, logMissing: false))
            return;

        state.IsClosed = eyeClosed;
        state.IsCompleteBlink = !eyeClosed;
        _sprite.LayerSetVisible(ent, layerIndex, eyeClosed);
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
        if (!_spriteQuery.TryComp(ent.Comp.Body, out SpriteComponent? sprite))
            return;

        foreach (var eyelidState in ent.Comp.Eyelids)
            ChangeEyeState((ent.Comp.Body.Value, sprite), eyelidState, eyeClosed);
    }


    /// <summary>
    /// Creates (or recreates) eyelid layers and initializes the client blinking component.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="body"></param>
    private void InitEyeBlinking(Entity<EyeBlinkingComponent> ent, EntityUid body)
    {
        if (!_spriteQuery.TryComp(body, out SpriteComponent? sprite))
            return;

        if (!_sprite.LayerMapTryGet((body, sprite), HumanoidVisualLayers.Eyelids, out var targetLayer, false))
            return;

        ent.Comp.Body = body;

        InitEyelidsLayers(ent, body, targetLayer);

        // Initialize and randomize the blink timer.
        ResetBlink(ent);

        ChangeEyesState(ent, ent.Comp.Status != BlinkStatus.Normal);

        ent.Comp.LastStatus = ent.Comp.Status;
    }

    private void UpdateEyelidsColor(Entity<EyeBlinkingComponent> ent, EntityUid body)
    {
        if (ent.Comp.EyelidsColor is not { } eyelidsColor)
            return;

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        // Update all existing eyelid layers.
        var i = 0;
        while (_sprite.LayerMapTryGet((body, sprite), $"{LayerPrefix}-{i}", out var layerIndex, logMissing: false))
        {
            _sprite.LayerSetColor((body, sprite), layerIndex, eyelidsColor);
            i++;
        }
    }

    private void InitEyelidsLayers(Entity<EyeBlinkingComponent> ent, EntityUid body, int eyelidsLayer)
    {
        if (!_spriteQuery.TryComp(body, out SpriteComponent? comp))
            return;

        // Remove existing eyelid layers by their expected mapping
        var i = 0;
        while (_sprite.LayerMapRemove((body, comp), $"{LayerPrefix}-{i}"))
        {
            i++;
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

        // If the entity has a specific eyelid color defined after organData init, use that color instead of the default white.
        var eyelidColor = ent.Comp.EyelidsColor ?? Color.White;

        var rsiCollection = rsiRes.RSI;

        DisplacementDataPrototype? displacementProto = null;

        if (VisualOrganQuery.TryComp(ent.Owner, out VisualOrganComponent? visualOrgan)
            && visualOrgan.Displacement != null)
        {
            ProtoMan.Resolve(visualOrgan.Displacement, out displacementProto);
        }

        // Creates a new layer for each eyelid state defined in the RSI.
        i = 0;
        foreach (var state in rsiCollection)
        {
            if (state.StateId.Name is not { } name
                || !name.StartsWith(ent.Comp.StatePrefix))
                continue;

            var specifier = new SpriteSpecifier.Rsi(rsiPath.Value, state.StateId.Name);
            var layerId = $"{LayerPrefix}-{i}";
            if (!_sprite.LayerMapTryGet((body, comp), layerId, out var layerIndex, false))
            {
                layerIndex = _sprite.AddLayer((body, comp), specifier, eyelidsLayer + i + 1);
                _sprite.LayerMapSet((body, comp), layerId, layerIndex);
            }
            _sprite.LayerSetSprite((body, comp), layerIndex, specifier);
            _sprite.LayerSetColor((body, comp), layerIndex, eyelidColor);
            _sprite.LayerSetVisible((body, comp), layerIndex, false);

            if (displacementProto != null)
                _displacement.TryAddDisplacement(displacementProto.Displacement, (body, comp), layerIndex, layerId, out _);

            ent.Comp.Eyelids.Add(new EyelidState(layerId));
            i++;
        }
    }
    #endregion Internal

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
            if (comp.Status != BlinkStatus.Normal)
                continue;

            if (!_spriteQuery.TryComp(comp.Body, out SpriteComponent? sprite))
                continue;

            // If a blink is currently in progress, check the scheduled times for each eyelid and update their states accordingly.
            if (comp.BlinkInProgress)
            {
                foreach (var eyelidState in comp.Eyelids)
                {
                    // If the eyelid is not closed and the current time has reached or passed the scheduled close time, close the eyelid.
                    if (!eyelidState.IsClosed && curTime >= eyelidState.ScheduledCloseTime && eyelidState.IsCompleteBlink == false)
                    {
                        ChangeEyeState((comp.Body.Value, sprite), eyelidState, true);
                    }
                    // If the eyelid is closed and the current time has reached or passed the scheduled open time, open the eyelid.
                    else if (eyelidState.IsClosed && curTime >= eyelidState.ScheduledOpenTime)
                    {
                        ChangeEyeState((comp.Body.Value, sprite), eyelidState, false);
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

    /// <summary>
    /// Client-side state updates: force eye visibility to change.
    /// Works for both prediction (all shared code, based off of the immediate predicted values as they happen)
    /// and for authoritative state (based off of last state from the server in AfterAutoHandleState)
    /// </summary>
    protected override void StatusChanged(Entity<EyeBlinkingComponent> ent, BlinkStatus oldValue)
    {
        if (ent.Comp.Status == BlinkStatus.Normal)
        {
            // We can see again! Open our eyes and restart the cycle.
            ChangeEyesState(ent, false);
            ResetBlink(ent);
        }
        else if (oldValue == BlinkStatus.Normal && !ent.Comp.Status.HasFlag(BlinkStatus.Dead))
        {
            // Eyes need to close (unless we're dead!)
            ChangeEyesState(ent, true);
        }
    }
}
