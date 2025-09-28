using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    #region Player data
    /*
     * Player data 🌟Starlight🌟
     */
    public async Task<StarLightModel.PlayerDataDTO?> GetPlayerDataDTOForAsync(NetUserId userId, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);
        return await db.DbContext.PlayerData
            .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);
    }
    public async Task SetPlayerDataForAsync(NetUserId userId, StarLightModel.PlayerDataDTO data, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);

        var obj = await db.DbContext.PlayerData
            .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);
        if (obj != null)
        {
            obj.Balance = data.Balance;
            obj.GhostTheme = data.GhostTheme;
        }
        else
            db.DbContext.PlayerData.Add(data);

        await db.DbContext.SaveChangesAsync();
    }
    #endregion

}