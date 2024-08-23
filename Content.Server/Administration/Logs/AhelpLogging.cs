using System;
using System.Threading.Tasks;
using Content.Server.Database;

namespace Content.Server.Administration.Logs
{
    public sealed class AhelpLogging
    {
        private readonly IServerDbManager _dbManager;

        public AhelpLogging(IServerDbManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task LogAhelpMessageAsync(int ahelpRound,
            Guid ahelpTarget,
            Guid sender,
            int senderEntity,
            bool isAdminned,
            bool targetOnline,
            string message,
            string roundStatus,
            DateTime timeSent)
        {
            // Find or create the AhelpExchange
            var ahelpExchange = await _dbManager.GetAhelpExchangeAsync(ahelpRound, ahelpTarget);

            if (ahelpExchange == null)
            {
                ahelpExchange = new AhelpExchange
                {
                    AhelpRound = ahelpRound,
                    AhelpTarget = ahelpTarget
                };
                await _dbManager.AddAhelpExchangeAsync(ahelpExchange);
            }

            // Create a new AhelpMessage and log it
            var ahelpMessage = new AhelpMessage
            {
                AhelpId = ahelpExchange.AhelpId,
                Id = await GenerateUniqueMessageIdAsync(ahelpExchange.AhelpId),
                SentAt = DateTime.UtcNow,
                RoundStatus = roundStatus,
                Sender = sender,
                SenderEntity = senderEntity,
                IsAdminned = isAdminned,
                TargetOnline = targetOnline,
                Message = message,
                TimeSent = timeSent
            };

            await _dbManager.AddAhelpMessageAsync(ahelpMessage);
        }

        private async Task<int> GenerateUniqueMessageIdAsync(int exchangeId)
        {
            var maxId = await _dbManager.GetMaxMessageIdForExchange(exchangeId);
            return maxId + 1;
        }

        // This is not used but it might be used in the future
        public async Task LogAhelpParticipantAsync(int ahelpId, int playerId)
        {
            var existingParticipant = await _dbManager.GetAhelpParticipantAsync(ahelpId, playerId);

            if (existingParticipant == null)
            {
                var ahelpParticipant = new AhelpParticipant
                {
                    AhelpId = ahelpId,
                    PlayerId = playerId
                };

                await _dbManager.AddAhelpParticipantAsync(ahelpParticipant);
            }
        }
    }
}
