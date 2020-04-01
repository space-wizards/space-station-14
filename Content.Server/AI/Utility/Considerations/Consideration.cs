using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;

namespace Content.Server.AI.Utility.Considerations
{
    public abstract class Consideration
    {
        protected IResponseCurve Curve { get; }

        public Consideration(IResponseCurve curve)
        {
            Curve = curve;
        }

        public abstract float GetScore(Blackboard context);

        public float ComputeResponseCurve(float score)
        {
            return Curve.GetResponse(score);
        }
    }
}
