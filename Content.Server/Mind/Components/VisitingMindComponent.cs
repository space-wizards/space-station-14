namespace Content.Server.Mind.Components
{
    [RegisterComponent]
    public sealed partial class VisitingMindComponent : Component
    {
        [ViewVariables]
        public EntityUid? MindId;
    }

    public sealed class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
