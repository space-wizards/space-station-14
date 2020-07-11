using System;

namespace Content.Server.AI.WorldState.States.Utility
{
    public sealed class StoredStateIsNullState : PlanningStateData<Type>
    {
        public override string Name => "StoredStateIsNull";
        public override void Reset()
        {
            Value = null;
        }
    }
}