using Content.Shared.Whitelist;

namespace Content.Server.NukeOps
{
    [RegisterComponent]
    public sealed class WarDeclaratorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("message")]
        public string Message { get; set; } = string.Empty;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxMessageLength")]
        public int MaxMessageLength { get; set; } = 255;
    }
}
