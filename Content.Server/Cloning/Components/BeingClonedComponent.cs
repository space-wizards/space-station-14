namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed partial class BeingClonedComponent : Component
    {
        [ViewVariables]
        public Mind.Mind? Mind = default;

        [ViewVariables]
        public EntityUid Parent;
    }
}
