using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Disposal.MailingUnit
{
    /// <summary>
    ///     Message data sent from client to server when the mailing units target is updated.
    /// </summary>
    [Serializable, NetSerializable]
    public class UiTargetUpdateMessage : BoundUserInterfaceMessage
    {
        public readonly string Target;

        public UiTargetUpdateMessage(string target)
        {
            Target = target;
        }
    }
}
