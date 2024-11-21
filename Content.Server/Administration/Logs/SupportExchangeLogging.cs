using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Logs;

/// <summary>
/// Provides functionality for logging support exchanges, including
/// interactions between players and administrators, and mentors (when implemented).
/// </summary>
public sealed class SupportExchangeLogging
{
    private readonly IServerDbManager _dbManager;
    private readonly Dictionary<(int supportRound, Guid supportTargetId), SupportExchangeQueue> _exchanges = new();

    public SupportExchangeLogging(IServerDbManager dbManager)
    {
        _dbManager = dbManager;
    }

    /// <summary>
    /// Creates a support exchange for the given round and target id if it doesn't exist. <br/>
    /// Saves to be stored messages in a queue belonging to the exchange and stores all messages in that queue
    /// when the exchange has been stored into the database
    /// </summary>
    public async void LogSupportMessageAsync(
        int supportRound,
        string roundStatus,
        DateTime timeSent,
        bool adminsOnline,
        Guid senderId,
        EntityUid? senderEntity,
        string? senderEntityName,
        bool isAdminned,
        Guid supportTargetId,
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

        var exchangeQueue = _exchanges.GetOrNew(key);
        exchangeQueue.MessageQueue.Enqueue(supportMessage);

        if (!exchangeQueue.Stored)
        {
            var newExchange = new SupportExchange
            {
                SupportRound = supportRound,
                SupportTargetPlayer = supportTargetId,
            };

            try
            {
                var id = await _dbManager.AddSupportExchangeAsync(newExchange);
                _exchanges[key].SupportExchangeId = id;
                _exchanges[key].Stored = true;

                await StoreMessages(exchangeQueue);
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e);
            }
            return;
        }

        try
        {
            await StoreMessages(exchangeQueue);
        }
        catch(DbUpdateException e)
        {
            Console.WriteLine($"Error saving Support Message: {e.Message}");
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
            await _dbManager.AddSupportMessageAsync(message);
        }
    }

}
