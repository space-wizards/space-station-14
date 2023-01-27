using Robust.Shared.Network;

namespace Content.Server.TraitorDeathMatch.Components
{
    [RegisterComponent]
    public sealed partial class TraitorDeathMatchReliableOwnerTagComponent : Component
    {
        [ViewVariables]
        public NetUserId? UserId { get; set; }
    }
}

