using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Research;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheComponent : SharedLatheComponent, IAttackHand, IAttackBy
    {
        private Dictionary<LatheMaterial, uint> _actualMaterialStorage;

        bool IAttackHand.AttackHand(AttackHandEventArgs args)
        {
            args.User.TryGetComponent(out BasicActorComponent actor);

            if (actor == null) return false;

            SendNetworkMessage(new LatheMenuOpenMessage(), actor.playerSession?.ConnectedClient);

            return false;
        }

        bool IAttackBy.AttackBy(AttackByEventArgs args)
        {
            return InsertMaterial(args.AttackWith);
        }

        bool InsertMaterial(IEntity entity)
        {
            entity.TryGetComponent(out LatheMaterialComponent material);

            if (material == null) return false;
            if (!AcceptsMaterial(material.Material)) return false;

            _actualMaterialStorage[material.Material] += material.Quantity;

            SendNetworkMessage(new LatheMaterialsUpdateMessage(_actualMaterialStorage));

            entity.Delete();

            return true;
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {

        }
    }
}
