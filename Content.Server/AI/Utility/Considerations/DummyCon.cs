using Content.Server.AI.WorldState;

namespace Content.Server.AI.Utility.Considerations
{
    public sealed class DummyCon : Consideration
    {
        protected override float GetScore(Blackboard context) => 1.0f;
    }
}
