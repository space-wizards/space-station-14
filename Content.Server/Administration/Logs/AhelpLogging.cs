using System;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;

namespace Content.Server.Administration.Logs
{
    public sealed class AhelpLogging
    {
        private readonly IServerDbManager _dbManager;

        public AhelpLogging(IServerDbManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task LogAhelpMessageAsync(
            string serverName,
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

            //Convert SenderUID to int so Db doesnt have a stroke
            var senderEntityInt = senderEntity != null
                ? (int?) (int) senderEntity
                : null;

            // Find or create the AhelpExchange
            //Currently new exchanges are created on different rounds, ie need to create a way to determine if the player
            //is still in the session.
            var ahelpExchange = await _dbManager.GetAhelpExchangeAsync(ahelpRound, ahelpTarget, serverName);

            if (ahelpExchange == null)
            {
                ahelpExchange = new AhelpExchange
                {
                    AhelpRound = ahelpRound,
                    AhelpTarget = ahelpTarget,
                    ServerName = serverName
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
