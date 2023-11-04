using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class EditSolutionsEuiState : EuiStateBase
    {
        public readonly NetEntity Target;
        public readonly Dictionary<string, Solution>? Solutions;

        public EditSolutionsEuiState(NetEntity target, Dictionary<string, Solution>? solutions)
        {
            Target = target;
            Solutions = solutions;
        }
    }
}
