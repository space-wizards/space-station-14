using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using Content.Shared.Alert;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.StatusEffect
{
    public class SharedStatusEffectsSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StatusEffectsComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<StatusEffectsComponent, ComponentHandleState>(OnHandleState);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _gameTiming.CurTime;
            foreach (var status in EntityManager.EntityQuery<StatusEffectsComponent>(false))
            {
                if (status.ActiveEffects.Count == 0) continue;
                foreach (var state in status.ActiveEffects.ToArray())
                {
                    // if we're past the end point of the effect
                    if (_gameTiming.CurTime > state.Value.Cooldown.Item2)
                    {
                        TryRemoveStatusEffect(status.Owner.Uid, state.Key, status);
                    }
                }
            }
        }

        private void OnGetState(EntityUid uid, StatusEffectsComponent component, ref ComponentGetState args)
        {
            args.State = new StatusEffectsComponentState(component.ActiveEffects, component.AllowedEffects);
        }

        private void OnHandleState(EntityUid uid, StatusEffectsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is StatusEffectsComponentState state)
            {
                component.AllowedEffects = state.AllowedEffects;

                foreach (var effect in state.ActiveEffects)
                {
                    // don't bother with anything if we already have it
                    if (component.ActiveEffects.ContainsKey(effect.Key))
                    {
                        component.ActiveEffects[effect.Key] = effect.Value;
                        continue;
                    }

                    var time = effect.Value.Cooldown.Item2 - effect.Value.Cooldown.Item1;
                    TryAddStatusEffect(uid, effect.Key, time);
                }
            }
        }

        /// <summary>
        ///     Tries to add a status effect to an entity, with a given component added as well.
        /// </summary>
        /// <param name="uid">The entity to add the effect to.</param>
        /// <param name="key">The status effect ID to add.</param>
        /// <param name="time">How long the effect should last for.</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <param name="alerts">The alerts component to modify, if the status effect has an alert.</param>
        /// <returns>False if the effect could not be added or the component already exists, true otherwise.</returns>
        /// <typeparam name="T">The component type to add and remove from the entity.</typeparam>
        public bool TryAddStatusEffect<T>(EntityUid uid, string key, TimeSpan time,
            StatusEffectsComponent? status=null,
            SharedAlertsComponent? alerts=null)
            where T: Component, new()
        {
            if (EntityManager.HasComponent<T>(uid))
                return false;
            if (!Resolve(uid, ref status, true))
                return false;

            Resolve(uid, ref alerts, false);

            if (TryAddStatusEffect(uid, key, time, status, alerts))
            {
                var comp = EntityManager.AddComponent<T>(uid);
                status.ActiveEffects[key].RelevantComponent = comp.Name;
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
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <param name="alerts">The alerts component to modify, if the status effect has an alert.</param>
        /// <returns>False if the effect could not be added, or if the effect already existed.</returns>
        /// <remarks>
        ///     This obviously does not add any actual 'effects' on its own. Use the generic overload,
        ///     which takes in a component type, if you want to automatically add and remove a component.
        /// </remarks>
        public bool TryAddStatusEffect(EntityUid uid, string key, TimeSpan time,
            StatusEffectsComponent? status=null,
            SharedAlertsComponent? alerts=null)
        {
            if (!Resolve(uid, ref status, true))
                return false;
            if (!CanApplyEffect(uid, key, status))
                return false;

            Resolve(uid, ref alerts, false);

            // we already checked if it has the index in CanApplyEffect so a straight index and not tryindex here
            // is fine
            var proto = _prototypeManager.Index<StatusEffectPrototype>(key);

            ValueTuple<TimeSpan, TimeSpan> cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + time);
            status.ActiveEffects.Add(key, new StatusEffectState(cooldown, null));

            if (proto.Alert != null && alerts != null)
            {
                alerts.ShowAlert(proto.Alert.Value, cooldown: cooldown);
            }

            status.Dirty();
            // event?
            return true;
        }

        /// <summary>
        ///     Attempts to remove a status effect from an entity.
        /// </summary>
        /// <param name="uid">The entity to remove an effect from.</param>
        /// <param name="key">The effect ID to remove.</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <param name="alerts">The alerts component to modify, if the status effect has an alert.</param>
        /// <returns>False if the effect could not be removed, true otherwise.</returns>
        /// <remarks>
        ///     Obviously this doesn't automatically clear any effects a status effect might have.
        ///     That's up to the removed component to handle itself when it's removed.
        /// </remarks>
        public bool TryRemoveStatusEffect(EntityUid uid, string key,
            StatusEffectsComponent? status=null,
            SharedAlertsComponent? alerts=null)
        {
            if (!Resolve(uid, ref status, false))
                return false;
            if (!status.ActiveEffects.ContainsKey(key))
                return false;
            if (!_prototypeManager.TryIndex<StatusEffectPrototype>(key, out var proto))
                return false;

            Resolve(uid, ref alerts, false);

            var state = status.ActiveEffects[key];
            if (state.RelevantComponent != null)
            {
                var type = _componentFactory.GetRegistration(state.RelevantComponent).Type;
                EntityManager.RemoveComponent(uid, type);
            }

            if (proto.Alert != null && alerts != null)
            {
                alerts.ClearAlert(proto.Alert.Value);
            }

            status.ActiveEffects.Remove(key);

            status.Dirty();
            // event?
            return true;
        }

        /// <summary>
        ///     Tries to remove all status effects from a given entity.
        /// </summary>
        /// <param name="uid">The entity to remove effects from.</param>
        /// <param name="status">The status effects component to change, if you already have it.</param>
        /// <param name="alerts">The alerts component to modify, if the status effect has an alert.</param>
        /// <returns>False if any status effects failed to be removed, true if they all did.</returns>
        public bool TryRemoveAllStatusEffects(EntityUid uid,
            StatusEffectsComponent? status = null,
            SharedAlertsComponent? alerts = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            Resolve(uid, ref alerts, false);

            bool failed = false;
            foreach (var effect in status.ActiveEffects)
            {
                if(!TryRemoveStatusEffect(uid, effect.Key, status, alerts))
                    failed = true;
            }

            return failed;
        }

        /// <summary>
        ///     Returns whether a given entity has the status effect active.
        /// </summary>
        /// <param name="uid">The entity to check on.</param>
        /// <param name="key">The status effect ID to check for</param>
        /// <param name="status">The status effect component, should you already have it.</param>
        public bool HasStatusEffect(EntityUid uid, string key,
            StatusEffectsComponent? status=null)
        {
            if (!Resolve(uid, ref status, true))
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
            if (!_prototypeManager.HasIndex<StatusEffectPrototype>(key))
                return false;
            if (!status.AllowedEffects.Contains(key))
                return false;

            // not our job!! handle updating it yourself, since different effects probably want to merge
            // active/adding effects differently.
            if (status.ActiveEffects.ContainsKey(key))
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
            if (!Resolve(uid, ref status, true))
                return false;

            if (!HasStatusEffect(uid, key, status))
                return false;

            var timer = status.ActiveEffects[key].Cooldown;
            timer.Item2 += time;

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
            if (!Resolve(uid, ref status, true))
                return false;

            if (!HasStatusEffect(uid, key, status))
                return false;

            var timer = status.ActiveEffects[key].Cooldown;

            // what on earth are you doing, Gordon?
            if (time > timer.Item2)
                return false;

            timer.Item2 -= time;

            return true;
        }

        // TODO remove/add time from an effect
    }
}
