using Robust.Shared.Network;

namespace Content.Server.TraitorDeathMatch.Components
{
    [RegisterComponent]
    public sealed class TraitorDeathMatchReliableOwnerTagComponent : Component
    {
        [ViewVariables]
        public NetUserId? UserId { get; set; }
    }
}

