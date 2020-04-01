using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState
{
    /// <summary>
    /// Basic StateDate, no frills
    /// </summary>
    public interface IAiState
    {
        void Setup(IEntity owner);
    }

    public interface IPlanningState
    {
        void Reset();
    }

    public interface ICachedState
    {
        void CheckCache();
    }

    /// <summary>
    /// The default class for state values. Also see CachedStateData and PlanningStateData
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StateData<T> : IAiState
    {
        public abstract string Name { get; }
        protected IEntity Owner { get; private set; }

        public void Setup(IEntity owner)
        {
            Owner = owner;
        }

        public abstract T GetValue();
    }

    /// <summary>
    /// This is state data that is transient and forgotten every time we re-plan
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PlanningStateData<T> : IAiState, IPlanningState
    {
        public abstract string Name { get; }
        protected IEntity Owner { get; private set; }
        protected T Value;

        public void Setup(IEntity owner)
        {
            Owner = owner;
        }

        public abstract void Reset();

        public T GetValue()
        {
            return Value;
        }

        public virtual void SetValue(T value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// This is state data that is cached for n seconds before being discarded. Mostly useful to get nearby components and store the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CachedStateData<T> : IAiState, ICachedState
    {
        public abstract string Name { get; }
        protected IEntity Owner { get; private set; }
        private bool _cached;
        protected T Value;
        private DateTime _lastCache = DateTime.Now;
        /// <summary>
        /// How long something stays in the cache before new values are retrieved
        /// </summary>
        protected float CacheTime { get; set; } = 2.0f;

        public void Setup(IEntity owner)
        {
            Owner = owner;
        }

        public void CheckCache()
        {
            if (!_cached || (DateTime.Now - _lastCache).TotalSeconds >= CacheTime)
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
                _lastCache = DateTime.Now;
            }

            return Value;
        }
    }
}
