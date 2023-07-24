namespace Content.Server.MassMedia.Components
{
    [RegisterComponent]
    public sealed class NewsReadComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int Test = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public int ArticleNum;
    }
}
