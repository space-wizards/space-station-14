using System;
using System.Collections.Generic;
using Content.Client.Chat.UI;
using Content.Shared.Chat;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager
    {
        ChatChannel ChannelFilters { get; }
        ChatSelectChannel SelectableChannels { get; }
        ChatChannel FilterableChannels { get; }

        void Initialize();

        void FrameUpdate(FrameEventArgs delta);

        void SetChatBox(ChatBox chatBox);

        void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble);

        /// <summary>
        /// Current chat box control. This can be modified, so do not depend on saving a reference to this.
        /// </summary>
        ChatBox? CurrentChatBox { get; }

        IReadOnlyDictionary<ChatChannel, int> UnreadMessages { get; }
        IReadOnlyList<StoredChatMessage> History { get; }
        int MaxMessageLength { get; }
        bool IsGhost { get; }

        /// <summary>
        /// Invoked when CurrentChatBox is resized (including after setting initial default size)
        /// </summary>
        event Action<ChatResizedEventArgs>? OnChatBoxResized;

        event Action<ChatPermissionsUpdatedEventArgs>? ChatPermissionsUpdated;
        event Action? UnreadMessageCountsUpdated;
        event Action<StoredChatMessage>? MessageAdded;
        event Action? FiltersUpdated;

        void ClearUnfilteredUnreads();
        void ChatBoxOnResized(ChatResizedEventArgs chatResizedEventArgs);
        void OnChatBoxTextSubmitted(ChatBox chatBox, ReadOnlyMemory<char> text, ChatSelectChannel channel);
        void OnFilterButtonToggled(ChatChannel channel, bool enabled);
    }

    public struct ChatPermissionsUpdatedEventArgs
    {
        public ChatSelectChannel OldSelectableChannels;
    }
}
