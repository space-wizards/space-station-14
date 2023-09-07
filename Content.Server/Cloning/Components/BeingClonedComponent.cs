using Content.Shared.Mind;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed partial class BeingClonedComponent : Component
    {
        [ViewVariables]
        public MindComponent? Mind = default;

        [ViewVariables]
        public EntityUid Parent;
    }
}
