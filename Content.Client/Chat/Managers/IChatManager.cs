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

        void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble);

        IReadOnlyDictionary<ChatChannel, int> UnreadMessages { get; }
        IReadOnlyList<StoredChatMessage> History { get; }
        int MaxMessageLength { get; }
        bool IsGhost { get; }

        event Action<ChatPermissionsUpdatedEventArgs>? ChatPermissionsUpdated;
        event Action? UnreadMessageCountsUpdated;
        event Action<StoredChatMessage>? MessageAdded;
        event Action? FiltersUpdated;

        void ClearUnfilteredUnreads();
        void OnFilterButtonToggled(ChatChannel channel, bool enabled);
    }

    public struct ChatPermissionsUpdatedEventArgs
    {
        public ChatSelectChannel OldSelectableChannels;
    }
}
