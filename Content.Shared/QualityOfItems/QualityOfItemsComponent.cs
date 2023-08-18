namespace Content.Shared.QualityOfItem
{
    [RegisterComponent]
    public sealed class QualityOfItemComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("quality")]
        public int Quality = 0;
    }
}
