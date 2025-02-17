using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Robust.Server.ServerStatus;

namespace Content.Server.Administration;

public sealed partial class ServerApi
{
    /// <summary>
    /// Get players and active admins list
    /// </summary>
    private async Task GetPlayers(IStatusHandlerContext context)
    {
        var playersList = new JsonArray();
        foreach (var player in _playerManager.Sessions)
        {
            playersList.Add(player.Name);
        }

        var adminMgr = await RunOnMainThread(IoCManager.Resolve<IAdminManager>);
        var adminsDict = new JsonObject();

        foreach (var admin in adminMgr.AllAdmins)
        {
            var adminData = adminMgr.GetAdminData(admin, true)!;
            adminsDict[admin.Name] = new JsonObject
            {
                ["isActive"] = adminData.Active,
                ["isStealth"] = adminData.Stealth,
                ["title"] = adminData.Title,
                ["flags"] = JsonSerializer.SerializeToNode(adminData.Flags.ToString().Split(", ")),
            };
        }

        var jObject = new JsonObject
        {
            ["players"] = playersList,
            ["admins"] = adminsDict
        };

        await context.RespondJsonAsync(jObject);
    }
}
