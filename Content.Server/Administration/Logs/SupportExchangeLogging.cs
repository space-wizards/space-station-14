using System.Threading.Tasks;
using Content.Server.Database;

namespace Content.Server.Administration.Logs
{
    public sealed class SupportExchangeLogging
    {
        private readonly IServerDbManager _dbManager;
        private readonly ServerDbEntryManager _serverDbEntryManager;

        public SupportExchangeLogging(IServerDbManager dbManager, ServerDbEntryManager serverDbEntryManager)
        {
            _dbManager = dbManager;
            _serverDbEntryManager = serverDbEntryManager;
        }

        public async Task LogSupportMessageAsync(
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
            // Fetch server ID from ServerDbEntryManager
            var serverEntity = await _serverDbEntryManager.ServerEntity;
            var serverId = serverEntity.Id;

            // Find or create the SupportExchange
            var supportExchange = await _dbManager.GetSupportExchangeAsync(supportRound, supportTargetId, serverId);

            if (supportExchange == null)
            {
                supportExchange = new SupportExchange
                {
                    SupportRound = supportRound,
                    SupportTarget = supportTargetId,
                    ServerId = serverId
                };
                await _dbManager.AddSupportExchangeAsync(supportExchange);
            }

            //set up the json
            var supportData = new SupportData(
                senderEntity != null ? (int)senderEntity : null,
                senderEntityName ?? string.Empty,
                isAdminned,
                adminsOnline,
                targetOnline,
                roundStatus
                );

            // Create a new SupportMessage
            var supportMessage = new SupportMessage
            {
                SupportExchangeId = supportExchange.SupportExchangeId,
                TimeSent = timeSent,
                PlayerUserId = senderId,
                SupportData = supportData,
                Message = message
            };

            await _dbManager.AddSupportMessageAsync(supportMessage);
        }
    }
}
