namespace Content.Server.FlavorText
{
    public sealed class FlavorTextComponent : Component
    {
        [DataField("content", required: true)] [ViewVariables(VVAccess.ReadWrite)]
        public string Content = "";
    }
}
