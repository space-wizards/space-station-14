namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class GasMixtureHolderComponent : Component, IGasMixtureHolder
    {
        [ViewVariables] [DataField("air")] public GasMixture Air { get; set; } = new GasMixture();
    }
}
