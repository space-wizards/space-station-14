using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Acts
{
    public sealed class DestructionEventArgs : EntityEventArgs { }

    public sealed class BreakageEventArgs : EntityEventArgs { }

    public interface IBreakAct
    {
        /// <summary>
        /// Called when object is broken
        /// </summary>
        void OnBreak(BreakageEventArgs eventArgs);
    }

    [UsedImplicitly]
    public sealed class ActSystem : EntitySystem
    {
        public void HandleDestruction(EntityUid owner)
        {
            var eventArgs = new DestructionEventArgs();

            RaiseLocalEvent(owner, eventArgs, false);
            QueueDel(owner);
        }

        public void HandleBreakage(EntityUid owner)
        {
            var eventArgs = new BreakageEventArgs();
            RaiseLocalEvent(owner, eventArgs, false);
            var breakActs = EntityManager.GetComponents<IBreakAct>(owner).ToList();
            foreach (var breakAct in breakActs)
            {
                breakAct.OnBreak(eventArgs);
            }
        }
    }
}
