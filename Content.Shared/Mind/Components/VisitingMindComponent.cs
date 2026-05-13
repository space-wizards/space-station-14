namespace Content.Shared.Mind.Components
{
    [RegisterComponent]
    public sealed partial class VisitingMindComponent : Component
    {
        [ViewVariables]
        public EntityUid? MindId;
    }

    public sealed partial class MindUnvisitedMessage : EntityEventArgs
    {
    }
}

