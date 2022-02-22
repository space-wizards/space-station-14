using System;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.Considerations.State
{
    /// <summary>
    /// Simple NullCheck on a StoredState
    /// </summary>
    public sealed class StoredStateEntityIsNullCon : Consideration
    {
        public StoredStateEntityIsNullCon Set(Type type, Blackboard context)
        {
            // Ideally we'd just use a variable but then if we were iterating through multiple AI at once it'd be
            // Stuffed so we need to store it on the AI's context.
            context.GetState<StoredStateIsNullState>().SetValue(type);
            return this;
        }

        protected override float GetScore(Blackboard context)
        {
            var stateData = context.GetState<StoredStateIsNullState>().GetValue();

            if (stateData == null)
            {
                return 0;
            }

            context.GetStoredState(stateData, out StoredStateData<EntityUid> state);
            return !state.GetValue().IsValid() ? 1.0f : 0.0f;
        }
    }
}
