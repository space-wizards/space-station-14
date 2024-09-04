using System.Threading.Tasks;
using Content.Server.Database;

namespace Content.Server.Administration.Logs
{
    public sealed class AhelpLogging
    {
        private readonly IServerDbManager _dbManager;
        private readonly ServerDbEntryManager _serverDbEntryManager;

        public AhelpLogging(IServerDbManager dbManager, ServerDbEntryManager serverDbEntryManager)
        {
            _dbManager = dbManager;
            _serverDbEntryManager = serverDbEntryManager;
        }

        public async Task LogAhelpMessageAsync(
            int ahelpRound,
            string roundStatus,
            DateTime timeSent,
            bool adminsOnline,
            Guid sender,
            EntityUid? senderEntity,
            string? senderEntityName,
            bool isAdminned,
            Guid ahelpTarget,
            bool targetOnline,
            string message
            )
        {

            //Convert SenderUID to int for Db compatibility
            var senderEntityInt = senderEntity != null
                ? (int?) (int) senderEntity
                : null;

            // Fetch server ID from ServerDbEntryManager
            var serverEntity = await _serverDbEntryManager.ServerEntity;
            var serverId = serverEntity.Id;

            // Find or create the AhelpExchange
            var ahelpExchange = await _dbManager.GetAhelpExchangeAsync(ahelpRound, ahelpTarget, serverId);

            if (ahelpExchange == null)
            {
                ahelpExchange = new AhelpExchange
                {
                    AhelpRound = ahelpRound,
                    AhelpTarget = ahelpTarget,
                    ServerId = serverId
                };
                await _dbManager.AddAhelpExchangeAsync(ahelpExchange);
            }

            // Create a new AhelpMessage and log it to that exchange
            var ahelpMessage = new AhelpMessage
            {
                AhelpId = ahelpExchange.AhelpId,
                Id = await GenerateUniqueMessageIdAsync(ahelpExchange.AhelpId),
                RoundStatus = roundStatus,
                TimeSent = timeSent,
                AdminsOnline = adminsOnline,
                Sender = sender,
                SenderEntity = senderEntityInt,
                SenderEntityName = senderEntityName,
                IsAdminned = isAdminned,
                TargetOnline = targetOnline,
                Message = message,
            };

            await _dbManager.AddAhelpMessageAsync(ahelpMessage);
        }

        private async Task<int> GenerateUniqueMessageIdAsync(int exchangeId)
        {
            var maxId = await _dbManager.GetMaxMessageIdForExchange(exchangeId);
            return maxId + 1;
        }
    }
}
