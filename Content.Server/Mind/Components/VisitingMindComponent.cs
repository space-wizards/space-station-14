namespace Content.Server.Mind.Components
{
    [RegisterComponent]
    public sealed class VisitingMindComponent : Component
    {
        [ViewVariables]
        public Mind Mind { get; set; } = default!;
    }

    public sealed class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
