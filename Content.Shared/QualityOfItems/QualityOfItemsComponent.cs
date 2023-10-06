namespace Content.Shared.QualityOfItem
{
    [RegisterComponent]
    public sealed partial class QualityOfItemComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("quality")]
        public int Quality = 0;
    }
}
