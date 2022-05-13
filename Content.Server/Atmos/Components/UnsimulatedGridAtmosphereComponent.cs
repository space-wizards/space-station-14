namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IAtmosphereComponent))]
    [Serializable]
    public sealed class UnsimulatedGridAtmosphereComponent : GridAtmosphereComponent
    {
        public override bool Simulated => false;
    }
}
