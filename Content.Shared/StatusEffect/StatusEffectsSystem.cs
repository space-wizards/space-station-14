using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.StatusEffect
{
    public sealed class StatusEffectsSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<StatusEffectsComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<StatusEffectsComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<StatusEffectsComponent, RejuvenateEvent>(OnRejuvenate);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _gameTiming.CurTime;
            var enumerator = EntityQueryEnumerator<ActiveStatusEffectsComponent, StatusEffectsComponent>();

            while (enumerator.MoveNext(out var uid, out _, out var status))
            {
                foreach (var state in status.ActiveEffects.ToArray())
                {
                    // if we're past the end point of the effect
                    if (curTime > state.Value.Cooldown.Item2)
                    {
                        TryRemoveStatusEffect(uid, state.Key, status);
                    }
                }
            }
        }

        private void OnGetState(EntityUid uid, StatusEffectsComponent component, ref ComponentGetState args)
        {
            // Using new(...) To avoid mispredictions due to MergeImplicitData. This will mean the server-side code is
            // slightly slower, and really this function should just be overridden by the client...
            args.State = new StatusEffectsComponentState(new(component.ActiveEffects), new(component.AllowedEffects));
        }

        private void OnHandleState(EntityUid uid, StatusEffectsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not StatusEffectsComponentState state)
                return;

            component.AllowedEffects.Clear();
            component.AllowedEffects.AddRange(state.AllowedEffects);

            // Remove non-existent effects.
            foreach (var effect in component.ActiveEffects.Keys)
            {
                if (!state.ActiveEffects.ContainsKey(effect))
                {
                    TryRemoveStatusEffect(uid, effect, component, remComp: false);
                }
            }

            foreach (var (key, effect) in state.ActiveEffects)
            {
                // don't bother with anything if we already have it
                if (component.ActiveEffects.ContainsKey(key))
                {
                    component.ActiveEffects[key] = new(effect);
                    continue;
                }

                var time = effect.Cooldown.Item2 - effect.Cooldown.Item1;

                TryAddStatusEffect(uid, key, time, true, component, effect.Cooldown.Item1);
                component.ActiveEffects[key].RelevantComponent = effect.RelevantComponent;
                // state handling should not add networked components, that is handled separately by the client game state manager.
            }
        }

        private void OnRejuvenate(EntityUid uid, StatusEffectsComponent component, RejuvenateEvent args)
        {
            TryRemoveAllStatusEffects(uid, component);
        }

        /// <summary>
        ///     Tries to add a status effect to an entity, with a given component added as well.
        /// </summary>
        /// <param name="uid">The entity to add the effect to.</param>
        /// <param name="key">The status effect ID to add.</param>
        /// <param name="time">How long the effect should last for.</param>
        /// <param name="refresh">The status effect cooldown should be refreshed (true) or accumulated (false).</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <returns>False if the effect could not be added or the component already exists, true otherwise.</returns>
        /// <typeparam name="T">The component type to add and remove from the entity.</typeparam>
        public bool TryAddStatusEffect<T>(EntityUid uid, string key, TimeSpan time, bool refresh,
            StatusEffectsComponent? status = null)
            where T : IComponent, new()
        {
            if (!Resolve(uid, ref status, false))
                return false;

            if (TryAddStatusEffect(uid, key, time, refresh, status))
            {
                // If they already have the comp, we just won't bother updating anything.
                if (!EntityManager.HasComponent<T>(uid))
                {
                    var comp = EntityManager.AddComponent<T>(uid);
                    status.ActiveEffects[key].RelevantComponent = _componentFactory.GetComponentName(comp.GetType());
                }
                return true;
            }

            return false;
        }

        public bool TryAddStatusEffect(EntityUid uid, string key, TimeSpan time, bool refresh, string component,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            if (TryAddStatusEffect(uid, key, time, refresh, status))
            {
                // If they already have the comp, we just won't bother updating anything.
                if (!EntityManager.HasComponent(uid, _componentFactory.GetRegistration(component).Type))
                {
                    var newComponent = (Component) _componentFactory.GetComponent(component);
                    EntityManager.AddComponent(uid, newComponent);
                    status.ActiveEffects[key].RelevantComponent = component;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to add a status effect to an entity with a certain timer.
        /// </summary>
        /// <param name="uid">The entity to add the effect to.</param>
        /// <param name="key">The status effect ID to add.</param>
        /// <param name="time">How long the effect should last for.</param>
        /// <param name="refresh">The status effect cooldown should be refreshed (true) or accumulated (false).</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <param name="startTime">The time at which the status effect started. This exists mostly for prediction
        /// resetting.</param>
        /// <returns>False if the effect could not be added, or if the effect already existed.</returns>
        /// <remarks>
        ///     This obviously does not add any actual 'effects' on its own. Use the generic overload,
        ///     which takes in a component type, if you want to automatically add and remove a component.
        ///
        ///     If the effect already exists, it will simply replace the cooldown with the new one given.
        ///     If you want special 'effect merging' behavior, do it your own damn self!
        /// </remarks>
        public bool TryAddStatusEffect(EntityUid uid, string key, TimeSpan time, bool refresh,
            StatusEffectsComponent? status = null, TimeSpan? startTime = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;
            if (!CanApplyEffect(uid, key, status))
                return false;

            // we already checked if it has the index in CanApplyEffect so a straight index and not tryindex here
            // is fine
            var proto = _prototypeManager.Index<StatusEffectPrototype>(key);

            var start = startTime ?? _gameTiming.CurTime;
            (TimeSpan, TimeSpan) cooldown = (start, start + time);

            if (HasStatusEffect(uid, key, status))
            {
                status.ActiveEffects[key].CooldownRefresh = refresh;
                if (refresh)
                {
                    //Making sure we don't reset a longer cooldown by applying a shorter one.
                    if ((status.ActiveEffects[key].Cooldown.Item2 - _gameTiming.CurTime) < time)
                    {
                        //Refresh cooldown time.
                        status.ActiveEffects[key].Cooldown = cooldown;
                    }
                }
                else
                {
                    //Accumulate cooldown time.
                    status.ActiveEffects[key].Cooldown.Item2 += time;
                }
            }
            else
            {
                status.ActiveEffects.Add(key, new StatusEffectState(cooldown, refresh, null));
                EnsureComp<ActiveStatusEffectsComponent>(uid);
            }

            if (proto.Alert != null)
            {
                var cooldown1 = GetAlertCooldown(uid, proto.Alert.Value, status);
                _alertsSystem.ShowAlert(uid, proto.Alert.Value, null, cooldown1);
            }

            Dirty(uid, status);
            RaiseLocalEvent(uid, new StatusEffectAddedEvent(uid, key));
            return true;
        }

        /// <summary>
        ///     Finds the maximum cooldown among all status effects with the same alert
        /// </summary>
        /// <remarks>
        ///     This is mostly for stuns, since Stun and Knockdown share an alert key. Other times this pretty much
        ///     will not be useful.
        /// </remarks>
        private (TimeSpan, TimeSpan)? GetAlertCooldown(EntityUid uid, ProtoId<AlertPrototype> alert, StatusEffectsComponent status)
        {
            (TimeSpan, TimeSpan)? maxCooldown = null;
            foreach (var kvp in status.ActiveEffects)
            {
                var proto = _prototypeManager.Index<StatusEffectPrototype>(kvp.Key);

                if (proto.Alert == alert)
                {
                    if (maxCooldown == null || kvp.Value.Cooldown.Item2 > maxCooldown.Value.Item2)
                    {
                        maxCooldown = kvp.Value.Cooldown;
                    }
                }
            }

            return maxCooldown;
        }

        /// <summary>
        ///     Attempts to remove a status effect from an entity.
        /// </summary>
        /// <param name="uid">The entity to remove an effect from.</param>
        /// <param name="key">The effect ID to remove.</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <param name="remComp">If true, status effect removal will also remove the relevant component. This option
        /// exists mostly for prediction resetting.</param>
        /// <returns>False if the effect could not be removed, true otherwise.</returns>
        /// <remarks>
        ///     Obviously this doesn't automatically clear any effects a status effect might have.
        ///     That's up to the removed component to handle itself when it's removed.
        /// </remarks>
        public bool TryRemoveStatusEffect(EntityUid uid, string key,
            StatusEffectsComponent? status = null, bool remComp = true)
        {
            if (!Resolve(uid, ref status, false))
                return false;
            if (!status.ActiveEffects.ContainsKey(key))
                return false;
            if (!_prototypeManager.TryIndex<StatusEffectPrototype>(key, out var proto))
                return false;

            var state = status.ActiveEffects[key];

            // There are cases where a status effect component might be server-only, so TryGetRegistration...
            if (remComp
                && state.RelevantComponent != null
                && _componentFactory.TryGetRegistration(state.RelevantComponent, out var registration))
            {
                var type = registration.Type;
                EntityManager.RemoveComponent(uid, type);
            }

            if (proto.Alert != null)
            {
                _alertsSystem.ClearAlert(uid, proto.Alert.Value);
            }

            status.ActiveEffects.Remove(key);
            if (status.ActiveEffects.Count == 0)
            {
                RemComp<ActiveStatusEffectsComponent>(uid);
            }

            Dirty(uid, status);
            RaiseLocalEvent(uid, new StatusEffectEndedEvent(uid, key));
            return true;
        }

        /// <summary>
        ///     Tries to remove all status effects from a given entity.
        /// </summary>
        /// <param name="uid">The entity to remove effects from.</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <returns>False if any status effects failed to be removed, true if they all did.</returns>
        public bool TryRemoveAllStatusEffects(EntityUid uid,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            bool failed = false;
            foreach (var effect in status.ActiveEffects)
            {
                if (!TryRemoveStatusEffect(uid, effect.Key, status))
                    failed = true;
            }

            Dirty(uid, status);
            return failed;
        }

        /// <summary>
        ///     Returns whether a given entity has the status effect active.
        /// </summary>
        /// <param name="uid">The entity to check on.</param>
        /// <param name="key">The status effect ID to check for</param>
        /// <param name="status">The status effect component, should you already have it.</param>
        public bool HasStatusEffect(EntityUid uid, string key,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;
            if (!status.ActiveEffects.ContainsKey(key))
                return false;

            return true;
        }

        /// <summary>
        ///     Returns whether a given entity can have a given effect applied to it.
        /// </summary>
        /// <param name="uid">The entity to check on.</param>
        /// <param name="key">The status effect ID to check for</param>
        /// <param name="status">The status effect component, should you already have it.</param>
        public bool CanApplyEffect(EntityUid uid, string key,
            StatusEffectsComponent? status = null)
        {
            // don't log since stuff calling this prolly doesn't care if we don't actually have it
            if (!Resolve(uid, ref status, false))
                return false;

            var ev = new BeforeStatusEffectAddedEvent(key);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
                return false;

            if (!_prototypeManager.TryIndex<StatusEffectPrototype>(key, out var proto))
                return false;
            if (!status.AllowedEffects.Contains(key) && !proto.AlwaysAllowed)
                return false;

            return true;
        }

        /// <summary>
        ///     Tries to add to the timer of an already existing status effect.
        /// </summary>
        /// <param name="uid">The entity to add time to.</param>
        /// <param name="key">The status effect to add time to.</param>
        /// <param name="time">The amount of time to add.</param>
        /// <param name="status">The status effect component, should you already have it.</param>
        public bool TryAddTime(EntityUid uid, string key, TimeSpan time,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            if (!HasStatusEffect(uid, key, status))
                return false;

            var timer = status.ActiveEffects[key].Cooldown;
            timer.Item2 += time;
            status.ActiveEffects[key].Cooldown = timer;

            if (_prototypeManager.TryIndex<StatusEffectPrototype>(key, out var proto)
                && proto.Alert != null)
            {
                (TimeSpan, TimeSpan)? cooldown = GetAlertCooldown(uid, proto.Alert.Value, status);
                _alertsSystem.ShowAlert(uid, proto.Alert.Value, null, cooldown);
            }

            Dirty(uid, status);
            return true;
        }

        /// <summary>
        ///     Tries to remove time from the timer of an already existing status effect.
        /// </summary>
        /// <param name="uid">The entity to remove time from.</param>
        /// <param name="key">The status effect to remove time from.</param>
        /// <param name="time">The amount of time to add.</param>
        /// <param name="status">The status effect component, should you already have it.</param>
        public bool TryRemoveTime(EntityUid uid, string key, TimeSpan time,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            if (!HasStatusEffect(uid, key, status))
                return false;

            var timer = status.ActiveEffects[key].Cooldown;

            // what on earth are you doing, Gordon?
            if (time > timer.Item2)
                return false;

            timer.Item2 -= time;
            status.ActiveEffects[key].Cooldown = timer;

            if (_prototypeManager.TryIndex<StatusEffectPrototype>(key, out var proto)
                && proto.Alert != null)
            {
                (TimeSpan, TimeSpan)? cooldown = GetAlertCooldown(uid, proto.Alert.Value, status);
                _alertsSystem.ShowAlert(uid, proto.Alert.Value, null, cooldown);
            }

            Dirty(uid, status);
            return true;
        }

        /// <summary>
        ///     Use if you want to set a cooldown directly.
        /// </summary>
        /// <remarks>
        ///     Not used internally; just sets it itself.
        /// </remarks>
        public bool TrySetTime(EntityUid uid, string key, TimeSpan time,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            if (!HasStatusEffect(uid, key, status))
                return false;

            status.ActiveEffects[key].Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + time);

            Dirty(uid, status);
            return true;
        }

        /// <summary>
        ///     Gets the cooldown for a given status effect on an entity.
        /// </summary>
        /// <param name="uid">The entity to check for status effects on.</param>
        /// <param name="key">The status effect to get time for.</param>
        /// <param name="time">Out var for the time, if it exists.</param>
        /// <param name="status">The status effects component to use, if any.</param>
        /// <returns>False if the status effect was not active, true otherwise.</returns>
        public bool TryGetTime(EntityUid uid, string key,
            [NotNullWhen(true)] out (TimeSpan, TimeSpan)? time,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false) || !HasStatusEffect(uid, key, status))
            {
                time = null;
                return false;
            }

            time = status.ActiveEffects[key].Cooldown;
            return true;
        }
    }

    /// <summary>
    ///     Raised on an entity before a status effect is added to determine if adding it should be cancelled.
    /// </summary>
    [ByRefEvent]
    public record struct BeforeStatusEffectAddedEvent(string Key, bool Cancelled=false);

    public readonly struct StatusEffectAddedEvent
    {
        public readonly EntityUid Uid;

        public readonly string Key;

        public StatusEffectAddedEvent(EntityUid uid, string key)
        {
            Uid = uid;
            Key = key;
        }
    }

    public readonly struct StatusEffectEndedEvent
    {
        public readonly EntityUid Uid;

        public readonly string Key;

        public StatusEffectEndedEvent(EntityUid uid, string key)
        {
            Uid = uid;
            Key = key;
        }
    }
}
