// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Security.Cryptography;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.SS220.Discord;

public sealed class DiscordPlayerManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private IServerDbManager _dbImpl = default!;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");
    }

    /// <summary>
    /// Проверка, генерация ключа для дискорда.
    /// Если валидация пройдена, то вернется пустая строка
    /// Если валидации не было, то вернется сгенерированный ключ
    /// </summary>
    /// <param name="playerData"></param>
    /// <returns></returns>
    public async Task<string> CheckAndGenerateKey(SessionData playerData)
    {
        try
        {
            var (validate, discordPlayer) = await _dbImpl.IsValidateDiscord(playerData.UserId);
            if (!validate)
            {
                if (discordPlayer != null)
                    return discordPlayer.HashKey;

                discordPlayer = new DiscordPlayer()
                {
                    CKey = playerData.UserName,
                    SS14Id = playerData.UserId,
                    HashKey = CreateSecureRandomString(8)
                };
                await _dbImpl.InsertDiscord(discordPlayer);
                return discordPlayer.HashKey;
            }
        }
        catch (Exception ex)
        {
            _sawmill.Log(LogLevel.Error, ex, "Ошибка во время проверки и генерации ключа");
            throw;
        }

        return string.Empty;
    }

    private static string CreateSecureRandomString(int count = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
}

