using System;
using Content.Server.GameObjects.Components.Body;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Network
{
    /// <summary>
    ///     Represents a "network" such as a bloodstream or electrical power that
    ///     is coordinated throughout an entire <see cref="BodyManagerComponent"/>.
    /// </summary>
    public abstract class BodyNetwork : IExposeData
    {
        [ViewVariables]
        public abstract string Name { get; }

        protected IEntity Owner { get; private set; }

        public virtual void ExposeData(ObjectSerializer serializer) { }

        public void OnAdd(IEntity entity)
        {
            Owner = entity;
            OnAdd();
        }

        protected virtual void OnAdd() { }

        public virtual void OnRemove() { }

        /// <summary>
        ///     Called every update by
        ///     <see cref="BodyManagerComponent.PreMetabolism"/>.
        /// </summary>
        public virtual void PreMetabolism(float frameTime) { }

        /// <summary>
        ///     Called every update by
        ///     <see cref="BodyManagerComponent.PostMetabolism"/>.
        /// </summary>
        public virtual void PostMetabolism(float frameTime) { }
    }

    public static class BodyNetworkExtensions
    {
        public static void TryAddNetwork(this IEntity entity, Type type)
        {
            if (!entity.TryGetComponent(out BodyManagerComponent body))
            {
                return;
            }

            body.EnsureNetwork(type);
        }

        public static void TryAddNetwork<T>(this IEntity entity) where T : BodyNetwork
        {
            if (!entity.TryGetComponent(out BodyManagerComponent body))
            {
                return;
            }

            body.EnsureNetwork<T>();
        }

        public static bool TryGetBodyNetwork(this IEntity entity, Type type, out BodyNetwork network)
        {
            network = null;

            return entity.TryGetComponent(out BodyManagerComponent body) &&
                   body.TryGetNetwork(type, out network);
        }

        public static bool TryGetBodyNetwork<T>(this IEntity entity, out T network) where T : BodyNetwork
        {
            entity.TryGetBodyNetwork(typeof(T), out var unCastNetwork);
            network = (T) unCastNetwork;
            return network != null;
        }
    }
}
