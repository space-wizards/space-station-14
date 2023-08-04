namespace Content.Server.Mind.Components
{
    [RegisterComponent]
    public sealed class VisitingMindComponent : Component
    {
        [ViewVariables] public Mind Mind { get; set; } = default!;

        protected override void OnRemove()
        {
            base.OnRemove();

            Mind?.UnVisit();
        }
    }

    public sealed class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
