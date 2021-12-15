using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.AI.WorldState
{
    /// <summary>
    /// Basic StateDate, no frills
    /// </summary>
    public interface IAiState
    {
        void Setup(EntityUid owner);
    }

    public interface IPlanningState
    {
        void Reset();
    }

    public interface ICachedState
    {
        void CheckCache();
    }

    public interface IStoredState {}

    /// <summary>
    /// The default class for state values. Also see CachedStateData and PlanningStateData
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StateData<T> : IAiState
    {
        public abstract string Name { get; }
        protected EntityUid Owner { get; private set; } = default!;

        public void Setup(EntityUid owner)
        {
            Owner = owner;
        }

        public abstract T? GetValue();
    }

    /// <summary>
    /// For when we want to set StateData but not reset it when re-planning actions
    /// Useful for group blackboard sharing or to avoid repeating the same action (e.g. bark phrases).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StoredStateData<T> : IAiState, IStoredState
    {
        // Probably not the best class name but couldn't think of anything better
        public abstract string Name { get; }
        private EntityUid Owner { get; set; }

        private T? _value;

        public void Setup(EntityUid owner)
        {
            Owner = owner;
        }

        public virtual void SetValue(T? value)
        {
            _value = value;
        }

        public T? GetValue()
        {
            return _value;
        }
    }

    /// <summary>
    /// This is state data that is transient and forgotten every time we re-plan
    /// e.g. "Current Target" gets updated for every action we consider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PlanningStateData<T> : IAiState, IPlanningState
    {
        public abstract string Name { get; }
        protected EntityUid Owner { get; private set; }
        protected T? Value;

        public void Setup(EntityUid owner)
        {
            Owner = owner;
        }

        public abstract void Reset();

        public T? GetValue()
        {
            return Value;
        }

        public virtual void SetValue(T? value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// This is state data that is cached for n seconds before being discarded.
    /// Mostly useful to get nearby components and store the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CachedStateData<T> : IAiState, ICachedState
    {
        public abstract string Name { get; }
        protected EntityUid Owner { get; private set; } = default!;
        private bool _cached;
        protected T Value = default!;
        private TimeSpan _lastCache = TimeSpan.Zero;
        /// <summary>
        /// How long something stays in the cache before new values are retrieved
        /// </summary>
        protected double CacheTime { get; set; } = 2.0f;

        public void Setup(EntityUid owner)
        {
            Owner = owner;
        }

        public void CheckCache()
        {
            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;

            if (!_cached || (curTime - _lastCache).TotalSeconds >= CacheTime)
            {
                _cached = false;
                return;
            }

            _cached = true;
        }

        /// <summary>
        /// When the cache is stale we'll retrieve the actual value and store it again
        /// </summary>
        protected abstract T GetTrueValue();

        public T GetValue()
        {
            CheckCache();
            if (!_cached)
            {
                Value = GetTrueValue();
                _cached = true;
                _lastCache = IoCManager.Resolve<IGameTiming>().CurTime;
            }

            return Value;
        }
    }
}
