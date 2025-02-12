namespace Content.Server.DetailExaminable
{
    [RegisterComponent]
    public sealed partial class DetailExaminableComponent : Component
    {
        [DataField(required: true)] [ViewVariables(required: true)]
        public string Content = "";
    }
}
