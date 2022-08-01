using JetBrains.Annotations;

namespace Content.Server.NPC.WorldState.States
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
