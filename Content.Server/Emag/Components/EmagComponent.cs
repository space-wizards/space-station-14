namespace Content.Server.Emag.Components
{
    [RegisterComponent]
    public sealed class EmagComponent : Component
    {
        [DataField("charges")]
        public int Charges = 5;
    }
}
