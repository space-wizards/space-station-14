using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore; // This is likely where ServerDbContext is defined

namespace Content.Server.Administration.Logs
{
    public sealed class AhelpLogging
    {
        [Dependency] private readonly ServerDbContext _dbContext = default!;

        public async Task LogAhelpMessageAsync(int ahelpRound,
            int ahelpTarget,
            int sender,
            int senderEntity,
            bool isAdminned,
            bool targetOnline,
            string message,
            RoundStatus roundStatus)
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

            // Create a new AhelpMessage
            var ahelpMessage = new AhelpMessage
            {
                AhelpId = ahelpExchange.AhelpId,
                SentAt = DateTime.UtcNow,
                RoundStatus = roundStatus,
                Sender = sender,
                SenderEntity = senderEntity,
                IsAdminned = isAdminned,
                TargetOnline = targetOnline,
                Message = message
            };

            _dbContext.AhelpMessages.Add(ahelpMessage);
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogAhelpParticipantAsync(int ahelpId, int playerId)
        {
            // Create a new AhelpParticipant
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
