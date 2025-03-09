using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Managers;

internal sealed partial class ChatManager
{
    /// <summary>
    /// Processes a message with formatting, markup and makes sure it gets sent out to the appropriate sessions/entities as designated by the chosen communication channel.
    /// </summary>
    /// <param name="logMessage">Whether the message should be logged in the admin logs. Defaults to true.</param>
    public void SendChannelMessage(ChatMessageWrapper message, bool logMessage = true)
    {
        var targetChannel = message.CommunicationChannel;
        var senderSession = message.SenderSession;
        var senderEntity = message.SenderEntity;
        var targetSessions = message.TargetSessions;

        #region Prep-Step

        // This section handles setting up the parameters and any other business that should happen before validation starts.

        // block if message was already sent by same entity and into same channel.
        var currentMessage = message;
        while (currentMessage.Parent != null)
        {
            if (currentMessage.Parent.CommunicationChannel == message.CommunicationChannel
                && currentMessage.SenderEntity == message.SenderEntity)
            {
                return;
            }

            currentMessage = currentMessage.Parent;
        }

        // Check for rate limiting if it's a client sending the message
        if (senderSession != null && HandleRateLimit(senderSession) != RateLimitStatus.Allowed)
            return;

        var messageContext = PrepareContext(message);

        #endregion

        #region Publisher Validation

        // This section handles validating the publisher based on ChatConditions, and passing on the message should the validation fail.

        // We also pass it on to any child channels that should be included.
        AlsoSendTo(message, targetChannel.AlwaysRelayedToChannels, logMessage);

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (!CanPublish(senderSession, targetChannel, messageContext))
        {
            AlsoSendTo(message, targetChannel.FallbackChannels, logMessage);

            // we failed publishing, no reason to proceed.
            return;
        }

        #endregion

        #region Consumers

        // This section handles sending out the message to consumers, whether that be sessions or entities.
        // This is done via consume conditions. Conditional modifiers may also be applied here for a subset of consumers.

        // Evaluate what clients should consume this message.
        var consumerConditions = new AnyChatCondition(targetChannel.ConsumeChatConditions);
        var filteredConsumers = new HashSet<ICommonSession>();
        var commonSessions = targetSessions ?? _playerManager.NetworkedSessions.ToHashSet();
        foreach (var commonSession in commonSessions)
        {
            var chatMessageConditionSubject = new ChatMessageConditionSubject(commonSession);
            if (consumerConditions.Check(chatMessageConditionSubject, messageContext))
            {
                filteredConsumers.Add(commonSession);
            }
        }

        // Conditional modifiers.
        // Modifiers necessitate sending different messages to groups of clients.
        // Therefore, if there are any that need to be applied,
        // the consumers are split off into separate consumer groups which each apply the list of modifiers.
        Dictionary<HashSet<ICommonSession>, List<ChatModifier>> chatConsumerGroups = new();

        EvaluateConditionalModifiers(filteredConsumers, 0, []);

        void EvaluateConditionalModifiers(
            HashSet<ICommonSession> sessions,
            int index,
            List<ChatModifier> inheritedModifiers
        )
        {
            for (var i = index; i < targetChannel.ConditionalModifiers.Count; i++)
            {
                var conditionalModifier = targetChannel.ConditionalModifiers[i];

                var chatCondition = new AnyChatCondition(conditionalModifier.Conditions);
                var filteredModifierConsumers = new HashSet<ICommonSession>();
                foreach (var commonSession in sessions)
                {
                    var chatMessageConditionSubject = new ChatMessageConditionSubject(commonSession);
                    if (chatCondition.Check(chatMessageConditionSubject, messageContext))
                    {
                        filteredModifierConsumers.Add(commonSession);
                    }
                }

                if (filteredConsumers.Count != 0)
                {
                    sessions.ExceptWith(filteredModifierConsumers);
                    var compiledModifiers = new List<ChatModifier>(conditionalModifier.Modifiers);
                    EvaluateConditionalModifiers(filteredModifierConsumers, i + 1, compiledModifiers);
                }

                if (sessions.Count == 0)
                    return;
            }

            if (sessions.Count != 0)
                chatConsumerGroups.Add(sessions, inheritedModifiers);
        }

        // No one heard a thing CHAT-TODO: Make sure to cover for objects!!
        if (chatConsumerGroups.Count == 0)
            return;

        // First, we apply all serverside modifiers that are unconditionally applied.
        foreach (var chatModifier in targetChannel.ServerModifiers)
        {
            var modified = chatModifier.ProcessChatModifier(message.MessageContent, messageContext);
            message.SetMessage(modified);
        }

        foreach (var chatConsumerGroup in chatConsumerGroups)
        {
            // Next, we apply any ConditionalModifiers for the consumer group.
            var consumerMessage = new FormattedMessage(message.MessageContent);
            foreach (var chatModifier in chatConsumerGroup.Value)
            {
                consumerMessage = chatModifier.ProcessChatModifier(consumerMessage, messageContext);
            }

            // Off the message goes!
            ChatFormattedMessageToHashset(
                message.MessageId,
                consumerMessage,
                targetChannel,
                chatConsumerGroup.Key.Select(x => x.Channel),
                senderEntity ?? EntityUid.Invalid,
                targetChannel.HideChat,
                true //CHAT-TODO: Process properly
            );

            //Logger.Debug(consumerMessage.ToMarkup());
        }

        // Sends an event to the entity that it spoke.
        // Systems using this event should exclusively use it for non-message-related functionality.
        // The message IS passed as an argument, but only if its contents needs to be used to determine functionality.
        // Still a bit iffy about even having this event...
        if (senderEntity != null)
        {
            var spokeEv = new EntitySpokeEvent(senderEntity.Value, message.ToString(), targetChannel);
            _entityManager.EventBus.RaiseLocalEvent(senderEntity.Value, spokeEv);
        }

        /* CHAT-TODO: get this part working.
        // Send out the message to any listening entities as well.
        if (consumeCollection.Conditions.Count > 0)
        {
            var getListenerEv = new GetListenerConsumerEvent();
            _entityManager.EventBus.RaiseEvent(EventSource.Local, ref getListenerEv);

            var baseEntityChatCondition = new AnyChatCondition(consumeCollection.Conditions);

            var filteredEntities = new HashSet<EntityUid>();
            foreach (var entityUid in getListenerEv.Entities)
            {
                if (baseEntityChatCondition.Check(new ChatMessageConditionSubject(entityUid), compiledChannelParameters))
                {
                    filteredEntities.Add(entityUid);
                }
            }

            filteredEntities.ExceptWith(exemptEntities);
            exemptEntities.UnionWith(filteredEntities);

            foreach (var consumerEntity in filteredEntities)
            {
                var listenerConsumeEv =
                    new ListenerConsumeEvent(targetChannel.ChatMedium, consumerMessage, compiledChannelParameters);

                _entityManager.EventBus.RaiseLocalEvent(consumerEntity, listenerConsumeEv);
            }
        */

        #endregion
    }

    private void AlsoSendTo(
        ChatMessageWrapper message,
        IEnumerable<ProtoId<CommunicationChannelPrototype>> otherChannels,
        bool logMessage = true
    )
    {
        foreach (var childChannel in otherChannels)
        {
            var channelPrototype = _prototypeManager.Index(childChannel);
            var newMessage = new ChatMessageWrapper(message, channelPrototype);
            SendChannelMessage(newMessage, logMessage);
        }
    }

    private static bool CanPublish(
        ICommonSession? senderSession,
        CommunicationChannelPrototype targetChannel,
        ChatMessageContext messageContext
    )
    {
        // If senderSession is null, it means the server is sending the message; no check is needed.
        if (senderSession == null)
            return true;

        // A channel without any publish conditions is only intended for the server as a publisher.
        if (targetChannel.PublishChatConditions.Count == 0)
        {
            return true;
        }

        var publishChatCondition = new AnyChatCondition(targetChannel.PublishChatConditions);
        var chatMessageConditionSubject = new ChatMessageConditionSubject(senderSession);
        var allowPublish = publishChatCondition.Check(chatMessageConditionSubject, messageContext);

        return allowPublish;
    }

    private static ChatMessageContext PrepareContext(ChatMessageWrapper message)
    {
        // Set the channel parameters, and supply any custom ones if necessary.
        var messageContext = new ChatMessageContext(message.CommunicationChannel.ChannelParameters, message.Context);

        // Includes the sender as a parameter for nodes that need it
        if (message.SenderEntity != null)
            messageContext[DefaultChannelParameters.SenderEntity] = message.SenderEntity.Value;

        if (message.SenderSession != null)
            messageContext[DefaultChannelParameters.SenderSession] = message.SenderSession;

        // Include a random seed based on the message's hashcode.
        // Since the message has yet to be formatted by anything, any child channels should get the same random seed.
        messageContext[DefaultChannelParameters.RandomSeed] = message.GetHashCode();
        return messageContext;
    }

    

}
