using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.TraitorDeathMatch.Components
{
    [RegisterComponent]
    public class TraitorDeathMatchReliableOwnerTagComponent : Component
    {
        [ViewVariables]
        public NetUserId? UserId { get; set; }
    }
}

