namespace Content.Server.VentCraw.Components
{
    [RegisterComponent]
    [Access(typeof(VentCrawTubeSystem))]
    public sealed class VentCrawEntryComponent : Component
    {
        public const string HolderPrototypeId = "VentCrawHolder";
    }
}
