using Content.Shared.Eui;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public class EditSolutionsEuiState : EuiStateBase
    {
        public readonly EntityUid Target;
        public readonly Dictionary<string, Solution>? Solutions;

        public EditSolutionsEuiState(EntityUid target, Dictionary<string, Solution>? solutions)
        {
            Target = target;
            Solutions = solutions;
        }
    }

    public static class EditSolutionsEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class Close : EuiMessageBase { }
    }
}
