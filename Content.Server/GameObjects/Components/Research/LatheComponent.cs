using System.Collections.Generic;
using Content.Server.GameObjects.Components.Materials;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Research;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheComponent : SharedLatheComponent, IAttackHand, IAttackBy
    {
        public Dictionary<StackType, uint> MaterialStorage;
        public List<StackType> AcceptedMaterials = new List<StackType>() {StackType.Metal, StackType.Glass};

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
            entity.TryGetComponent(out StackComponent stack);

            if (stack == null) return false;

            switch (stack.StackType)
            {
                case StackType.Metal:
                    return true;
                case StackType.Glass:
                    return true;
                default:
                    return false;
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {

        }
    }
}
