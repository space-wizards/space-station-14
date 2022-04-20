namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningConsoleComponent : Component
    {
        [ViewVariables]
        public EntityUid? GeneticScanner = null;
        [ViewVariables]
        public EntityUid? CloningPod = null;
    }
}
