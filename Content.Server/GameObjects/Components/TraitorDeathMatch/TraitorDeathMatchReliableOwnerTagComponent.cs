#nullable enable
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.TraitorDeathMatch
{
    [RegisterComponent]
    public class TraitorDeathMatchReliableOwnerTagComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "TraitorDeathMatchReliableOwnerTag";

        [ViewVariables]
        public NetUserId? UserId { get; set; }
    }
}

