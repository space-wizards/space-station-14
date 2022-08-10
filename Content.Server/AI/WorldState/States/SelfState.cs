using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States
{
    [UsedImplicitly]
    public sealed class SelfState : StateData<EntityUid>
    {
        public override string Name => "Self";

        public override EntityUid GetValue()
        {
            return Owner;
        }
    }
}
