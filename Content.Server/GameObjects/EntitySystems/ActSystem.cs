using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components behavior when being "triggered" by timer or other conditions
    /// </summary>
    public interface IDestroyAct
    {
        /// <summary>
        /// Called when one object is triggering some event
        /// </summary>
        void Destroy(DestructionEventArgs eventArgs);
    }

    public class DestructionEventArgs : EventArgs
    {
        public IEntity Owner { get; set; }
        public DamageType TypeOfDamage { get; set; }
        public int Damage { get; set; }
    }

    [UsedImplicitly]
    public sealed class ActSystem : EntitySystem
    {
        public void HandleDestruction(IEntity owner, int damage)
        {
            var eventArgs = new DestructionEventArgs
            {
                Owner = owner,
                TypeOfDamage = DamageType.Brute,
                Damage = damage
            };
            var destroyActs = owner.GetAllComponents<IDestroyAct>().ToList();

            foreach (var destroyAct in destroyActs)
            {
                destroyAct.Destroy(eventArgs);
            }
        }
    }
}