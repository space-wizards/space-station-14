using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;

namespace Content.Server.AI.Utility.Considerations
{
    public class DummyCon : Consideration
    {
        public DummyCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context) => 1.0f;
    }
}
