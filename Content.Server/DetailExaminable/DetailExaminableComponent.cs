namespace Content.Server.DetailExaminable
{
    [RegisterComponent]
    public sealed class DetailExaminableComponent : Component
    {
        [DataField("content", required: true)] [ViewVariables(VVAccess.ReadWrite)]
        public string Content = "";
    }
}
