using Content.Shared.Chat;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.Chat
{
    public class StoredChatMessage
    {
        // TODO Make me reflected with respect to MsgChatMessage

        /// <summary>
        ///     Client's own copies of chat messages used in filtering locally
        /// </summary>

        /// <summary>
        ///     Actual Message contents, i.e. words
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     Message channel, used for filtering
        /// </summary>
        public ChatChannel Channel { get; set; }

        /// <summary>
        ///     What to "wrap" the message contents with. Example is stuff like 'Joe says: "{0}"'
        /// </summary>
        public string MessageWrap { get; set; }

        /// <summary>
        /// The override color of the message
        /// </summary>
        public Color MessageColorOverride { get; set; }

        /// <summary>
        /// Whether the user has read this message at least once.
        /// </summary>
        public bool Read { get; set; }

        /// <summary>
        ///     Constructor to copy a net message into stored client variety
        /// </summary>
        public StoredChatMessage(MsgChatMessage netMsg)
        {
            Message = netMsg.Message;
            Channel = netMsg.Channel;
            MessageWrap = netMsg.MessageWrap;
            MessageColorOverride = netMsg.MessageColorOverride;
        }
    }
}
