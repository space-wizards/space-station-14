namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class FoamSolutionAreaEffectComponent : Component
    {
        public const string SolutionName = "solutionArea";

        [DataField("foamedMetalPrototype")] private string? _foamedMetalPrototype;

        // TODO: Make it "spawnonsmokedissipated" or smth component.
    }
}
