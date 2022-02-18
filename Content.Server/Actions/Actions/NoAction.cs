using Content.Shared.Actions.Behaviors;
using JetBrains.Annotations;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class NoAction : IInstantAction
    {

        public void DoInstantAction(InstantActionEventArgs args)
        {
        }
    }
}
