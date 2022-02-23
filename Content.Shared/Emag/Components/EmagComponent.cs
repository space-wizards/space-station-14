namespace Content.Shared.Emag.Components
{
    [RegisterComponent]
    public sealed class EmagComponent : Component
    {
        [DataField("maxCharges")]
        public int MaxCharges = 3;

        [DataField("charges")]
        public int Charges = 3;

        [DataField("rechargeTime")]
        public float RechargeTime = 90f;
        public float Accumulator = 0f;
    }
}
