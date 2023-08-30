namespace Content.Server.Visible
{
    [Flags]
    public enum VisibilityFlags : uint
    {
        None   = 0,
        Normal = 1 << 0,
        Ghost  = 1 << 1,
    }
}
