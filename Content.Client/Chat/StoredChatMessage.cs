using System;
using System.Collections.Generic;
using Content.Client.Interfaces.Chat;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Client.UserInterface.Controls;

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

        public StoredChatMessage(MsgChatMessage netMsg)
        {
            /// <summary>
            ///     Constructor to copy a net message into stored client variety
            /// </summary>
            
            Message = netMsg.Message;
            Channel = netMsg.Channel;
            MessageWrap = netMsg.MessageWrap;
        }
    }
}