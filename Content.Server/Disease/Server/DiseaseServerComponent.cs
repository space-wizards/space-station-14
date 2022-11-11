using Content.Shared.Disease;


namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    public sealed class DiseaseServerComponent : Component
    {
        /// <summary>
        /// Which diseases this server has information on.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> Diseases = new();
    }
}
