using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class EditSolutionsEuiState : EuiStateBase
    {
        public readonly EntityUid Target;
        public readonly Dictionary<string, Solution>? Solutions;

        public EditSolutionsEuiState(EntityUid target, Dictionary<string, Solution>? solutions)
        {
            Target = target;
            Solutions = solutions;
        }
    }
}
