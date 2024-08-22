using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Administration.Logs
{
    public sealed class AhelpLogging
    {
        [Dependency] private readonly ServerDbContext _dbContext = default!;

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
            var ahelpExchange = await _dbContext.AhelpExchanges
                .FirstOrDefaultAsync(e => e.AhelpRound == ahelpRound && e.AhelpTarget == ahelpTarget);

            if (ahelpExchange == null)
            {
                ahelpExchange = new AhelpExchange
                {
                    AhelpRound = ahelpRound,
                    AhelpTarget = ahelpTarget
                };
                _dbContext.AhelpExchanges.Add(ahelpExchange);
                await _dbContext.SaveChangesAsync();
            }

            // Create a new AhelpMessage and log it to a exchange
            var ahelpMessage = new AhelpMessage
            {
                AhelpId = ahelpExchange.AhelpId,
                SentAt = DateTime.UtcNow,
                RoundStatus = roundStatus,
                Sender = sender,
                SenderEntity = senderEntity,
                IsAdminned = isAdminned,
                TargetOnline = targetOnline,
                Message = message,
                TimeSent = timeSent
            };

            _dbContext.AhelpMessages.Add(ahelpMessage);
            await _dbContext.SaveChangesAsync();
        }

        //This is not used but it might be used in the future
        public async Task LogAhelpParticipantAsync(int ahelpId, int playerId)
        {
            // Check if the participant already exists
            var existingParticipant = await _dbContext.AhelpParticipants
                .FirstOrDefaultAsync(p => p.AhelpId == ahelpId && p.PlayerId == playerId);

            if (existingParticipant == null)
            {
                // Create a new AhelpParticipant if not already exists
                var ahelpParticipant = new AhelpParticipant
                {
                    AhelpId = ahelpId,
                    PlayerId = playerId
                };

                _dbContext.AhelpParticipants.Add(ahelpParticipant);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
