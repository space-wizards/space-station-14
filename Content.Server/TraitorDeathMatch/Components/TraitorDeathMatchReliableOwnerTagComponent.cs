using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.TraitorDeathMatch.Components
{
    [RegisterComponent]
    public sealed class TraitorDeathMatchReliableOwnerTagComponent : Component
    {
        [ViewVariables]
        public NetUserId? UserId { get; set; }
    }
}

