using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;

namespace Content.Server.AI.Utility.Considerations.State
{
    /// <summary>
    /// Simple NullCheck on a StoredState
    /// </summary>
    public sealed class StoredStateIsNullCon<T, U> : Consideration where T : StoredStateData<U>
    {
        public StoredStateIsNullCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var state = context.GetState<T>();
            if (state.GetValue() == null)
            {
                return 1.0f;
            }

            return 0.0f;
        }
    }
}