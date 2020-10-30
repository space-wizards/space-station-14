#nullable enable

namespace Content.Shared.Administration
{
    public sealed class AdminData
    {
        public const string DefaultTitle = "Admin";

        // Can be false if they're de-adminned with the ability to re-admin.
        public bool Active;
        public string? Title;
        public AdminFlags Flags;

        public bool HasFlag(AdminFlags flag)
        {
            return Active && (Flags & flag) == flag;
        }

        public bool CanViewVar()
        {
            return HasFlag(AdminFlags.VarEdit);
        }

        public bool CanAdminPlace()
        {
            return HasFlag(AdminFlags.Spawn);
        }

        public bool CanScript()
        {
            return HasFlag(AdminFlags.Host);
        }

        public bool CanAdminMenu()
        {
            return HasFlag(AdminFlags.Admin);
        }
    }
}
