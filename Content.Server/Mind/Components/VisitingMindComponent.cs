namespace Content.Server.Mind.Components
{
    [RegisterComponent]
    public sealed partial class VisitingMindComponent : Component
    {
        [ViewVariables]
        public EntityUid? MindId;

        [ViewVariables]
        public MindComponent? Mind;
    }

    public sealed class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
