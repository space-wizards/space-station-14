namespace Content.Server.Chat.Components
{

    /// <summary>
    /// Entities with this component will be able to hear chat messages via the ChatMessageHeardNearbyEvent.
    /// </summary>
    [RegisterComponent]
    public sealed class ChatListenerComponent : Component
    {
        /// <summary>
        ///  The hearing range of the entity
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hearingRange")]
        public int HearingRange = 3;
    }
}
