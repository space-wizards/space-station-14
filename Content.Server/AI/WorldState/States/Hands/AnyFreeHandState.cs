using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public sealed class AnyFreeHandState : StateData<bool>
    {
        public override string Name => "AnyFreeHand";
        public override bool GetValue()
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedHandsSystem>().TryGetEmptyHand(Owner, out _);
        }
    }
}
