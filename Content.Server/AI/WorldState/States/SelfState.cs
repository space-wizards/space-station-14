using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States
{
    [UsedImplicitly]
    public sealed class SelfState : StateData<IEntity>
    {
        public override string Name => "Self";

        public override IEntity GetValue()
        {
            return Owner;
        }
    }
}
