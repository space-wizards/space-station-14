using Content.Client.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace Content.Client.GameObjects
{
    public class HandsComponent : SharedHandsComponent, IHandsComponent
    {
        private readonly Dictionary<string, IEntity> hands = new Dictionary<string, IEntity>();

        public IEntity GetEntity(string index)
        {
            if (hands.TryGetValue(index, out var entity))
            {
                return entity;
            }

            return null;
        }

        public override void HandleComponentState(ComponentState state)
        {
            var cast = (HandsComponentState)state;
            hands.Clear();
            foreach (var hand in cast.Hands)
            {
                hands[hand.Key] = Owner.EntityManager.GetEntity(hand.Value);
            }
        }
    }
}
