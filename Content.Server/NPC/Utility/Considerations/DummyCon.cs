using Content.Server.NPC.WorldState;

namespace Content.Server.NPC.Utility.Considerations
{
    public sealed class DummyCon : Consideration
    {
        protected override float GetScore(Blackboard context) => 1.0f;
    }
}
