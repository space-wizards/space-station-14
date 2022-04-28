namespace Content.Shared.Emag.Components
{
    [RegisterComponent]
    public sealed class EmagComponent : Component
    {
        [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
        public int MaxCharges = 3;

        [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
        public int Charges = 3;

        [DataField("rechargeTime"), ViewVariables(VVAccess.ReadWrite)]
        public float RechargeTime = 90f;

        [DataField("accumulator")]
        public float Accumulator = 0f;
    }
}
