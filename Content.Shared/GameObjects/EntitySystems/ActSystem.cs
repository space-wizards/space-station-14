using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components behavior on getting destroyed.
    /// </summary>
    public interface IDestroyAct
    {
        /// <summary>
        /// Called when object is destroyed
        /// </summary>
        void OnDestroy(DestructionEventArgs eventArgs);
    }

    public class DestructionEventArgs : EventArgs
    {
        public IEntity Owner { get; set; }
    }

    public class BreakageEventArgs : EventArgs
    {
        public IEntity Owner { get; set; }
    }

    public interface IBreakAct
    {
        /// <summary>
        /// Called when object is broken
        /// </summary>
        void OnBreak(BreakageEventArgs eventArgs);
    }

    public interface IExAct
    {
        /// <summary>
        /// Called when explosion reaches the entity
        /// </summary>
        void OnExplosion(ExplosionEventArgs eventArgs);
    }

    public class ExplosionEventArgs : EventArgs
    {
        public EntityCoordinates Source { get; set; }
        public IEntity Target { get; set; }
        public ExplosionSeverity Severity { get; set; }
    }

    [UsedImplicitly]
    public sealed class ActSystem : EntitySystem
    {
        public void HandleDestruction(IEntity owner)
        {
            var eventArgs = new DestructionEventArgs
            {
                Owner = owner
            };

            var destroyActs = owner.GetAllComponents<IDestroyAct>().ToList();

            foreach (var destroyAct in destroyActs)
            {
                destroyAct.OnDestroy(eventArgs);
            }

            owner.Delete();
        }

        public void HandleExplosion(EntityCoordinates source, IEntity target, ExplosionSeverity severity)
        {
            var eventArgs = new ExplosionEventArgs
            {
                Source = source,
                Target = target,
                Severity = severity
            };
            var exActs = target.GetAllComponents<IExAct>().ToList();

            foreach (var exAct in exActs)
            {
                exAct.OnExplosion(eventArgs);
            }
        }

        public void HandleBreakage(IEntity owner)
        {
            var eventArgs = new BreakageEventArgs
            {
                Owner = owner,
            };
            var breakActs = owner.GetAllComponents<IBreakAct>().ToList();
            foreach (var breakAct in breakActs)
            {
                breakAct.OnBreak(eventArgs);
            }
        }
    }

    public enum ExplosionSeverity
    {
        Light,
        Heavy,
        Destruction,
    }
}
