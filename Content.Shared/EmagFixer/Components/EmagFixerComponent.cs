namespace Content.Shared.Emag.Components
{
    [RegisterComponent]
    public sealed class EmagFixerComponent : Component
    {
        [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
        public int MaxCharges = 4;

        [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
        public int Charges = 4;
    }
}
