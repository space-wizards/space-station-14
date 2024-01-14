using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class EditSolutionsEuiState : EuiStateBase
    {
        public readonly NetEntity Target;
        public readonly List<(string, NetEntity)>? Solutions;

        public EditSolutionsEuiState(NetEntity target, List<(string, NetEntity)>? solutions)
        {
            Target = target;
            Solutions = solutions;
        }
    }
}
