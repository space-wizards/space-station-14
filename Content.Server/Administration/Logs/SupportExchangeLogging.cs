using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Logs;

/// <summary>
/// Provides functionality for logging support exchanges, including
/// interactions between players and administrators, and mentors (when implemented).
/// </summary>
public sealed class SupportExchangeLogging
{
    private readonly IServerDbManager _dbManager;
    private readonly Dictionary<(int supportRound, NetUserId supportTargetId), SupportExchangeQueue> _exchanges = new();

    private readonly ISawmill _sawmill;

    public SupportExchangeLogging(IServerDbManager dbManager, ILogManager logManager)
    {
        _dbManager = dbManager;
        _sawmill = logManager.GetSawmill("support.logging");
    }

    /// <summary>
    /// Creates a support exchange for the given round and target id if it doesn't exist. <br/>
    /// Saves to be stored messages in a queue belonging to the exchange and stores all messages in that queue
    /// when the exchange has been stored into the database
    /// </summary>
    public async void LogSupportMessage(
        int supportRound,
        string roundStatus,
        DateTime timeSent,
        bool adminsOnline,
        NetUserId senderId,
        EntityUid? senderEntity,
        string? senderEntityName,
        bool isAdminned,
        NetUserId supportTargetId,
        bool targetOnline,
        string message
    )
    {
        var key = (supportRound, supportTargetId);

        var supportData = new SupportData(
            senderEntity?.ToString() ?? string.Empty,
            senderEntityName ?? string.Empty,
            isAdminned,
            adminsOnline,
            targetOnline,
            roundStatus
        );

        // Create a new SupportMessage
        var supportMessage = new SupportMessage
        {
            SupportExchangeId = 0,
            TimeSent = timeSent,
            PlayerUserId = senderId,
            SupportData = supportData,
            Message = message,
        };

        var exchangeQueue = _exchanges.GetOrNew(key, out var exists); // use out bool to prevent creating a new exchange if it already exists
        exchangeQueue.MessageQueue.Enqueue(supportMessage);
        if (!exchangeQueue.Stored && exists)
            return;

        try
        {
            if (!exchangeQueue.Stored && !exists)
            {
                var newExchange = new SupportExchange
                {
                    SupportRound = supportRound,
                    SupportTargetPlayer = supportTargetId,
                };

                var id = await _dbManager.AddSupportExchangeAsync(newExchange);
                exchangeQueue.SupportExchangeId = id;
                exchangeQueue.Stored = true;
            }

            await StoreMessages(exchangeQueue);
        }
        catch(DbUpdateException e)
        {
            _sawmill.Log(LogLevel.Error, e, "Error while saving support message");
        }
    }

    /// <summary>
    /// Stores all messages queued up in the given <see cref="SupportExchangeQueue"/>
    /// </summary>
    /// <param name="exchangeQueue">The queue to store messages for</param>
    private async Task StoreMessages(SupportExchangeQueue exchangeQueue)
    {
        while (exchangeQueue.MessageQueue.TryDequeue(out var message))
        {
            message.SupportExchangeId = exchangeQueue.SupportExchangeId;
            message.SupportMessageId = await _dbManager.GetNextMessageIdForExchange(message.SupportExchangeId);
            await _dbManager.AddSupportMessageAsync(message);
        }
    }

}
