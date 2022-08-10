using System.Linq;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public sealed class HandItemsState : StateData<List<EntityUid>>
    {
        public override string Name => "HandItems";
        public override List<EntityUid> GetValue()
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedHandsSystem>().EnumerateHeld(Owner).ToList();
        }
    }
}
