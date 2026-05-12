using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class EditSolutionsEuiState : EuiStateBase
    {
        public readonly NetEntity Target;
        public readonly List<(string, NetEntity)>? Solutions;
        public readonly GameTick Tick;

        public EditSolutionsEuiState(NetEntity target, List<(string, NetEntity)>? solutions, GameTick tick)
        {
            Target = target;
            Solutions = solutions;
            Tick = tick;
        }
    }
}
