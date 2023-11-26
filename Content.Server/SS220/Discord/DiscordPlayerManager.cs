// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Security.Cryptography;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Player;

namespace Content.Server.SS220.Discord;

public sealed class DiscordPlayerManager
{
    [Dependency] private readonly IServerDbManager _db = default!;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");
    }

    /// <summary>
    /// Проверка, генерация ключа для дискорда.
    /// </summary>
    /// <param name="playerData"></param>
    /// <returns></returns>
    public async Task<string> CheckAndGenerateKey(SessionData playerData)
    {
        try
        {
            var userId = playerData.UserId;

            var existing = await _db.GetAccountDiscordLink(playerData.UserId);

            // Привязки не существует, создаём.
            if (existing is null)
            {
                return await CreateKey(userId);
            }

            // Привязка существует и ключа нет, значит аккаунт уже прошёл привязку.
            if (string.IsNullOrWhiteSpace(existing.HashKey))
            {
                return string.Empty;
            }

            // Привязка существует и есть ключ, значит пользователь запрашивал привязку, но не использовал ключ.
            return existing.HashKey;
        }
        catch (Exception ex)
        {
            _sawmill.Log(LogLevel.Error, ex, "Ошибка во время проверки и генерации ключа");
            throw;
        }
    }

    private async Task<string> CreateKey(Guid userId)
    {
        var discordPlayer = new DiscordPlayer
        {
            SS14Id = userId,
            HashKey = CreateSecureRandomString(8)
        };

        await _db.InsertDiscord(discordPlayer);

        return discordPlayer.HashKey;
    }

    private static string CreateSecureRandomString(int count = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
}

