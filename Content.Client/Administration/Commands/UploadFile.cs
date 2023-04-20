using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Commands;

public sealed class UploadFile : IConsoleCommand
{
    public string Command => "uploadfile";
    public string Description => "Uploads a resource to the server.";
    public string Help => $"{Command} [relative path for the resource]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cfgMan = IoCManager.Resolve<IConfigurationManager>();

        if (!cfgMan.GetCVar(CCVars.ResourceUploadingEnabled))
        {
            shell.WriteError("Network Resource Uploading is currently disabled by the server.");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError("Wrong number of arguments!");
            return;
        }

        var path = new ResourcePath(args[0]).ToRelativePath();

        var dialog = IoCManager.Resolve<IFileDialogManager>();

        var filters = new FileDialogFilters(new FileDialogFilters.Group(path.Extension));
        await using var file = await dialog.OpenFile(filters);

        if (file == null)
        {
            shell.WriteError("Error picking file!");
            return;
        }

        var sizeLimit = cfgMan.GetCVar(CCVars.ResourceUploadingLimitMb);

        if (sizeLimit > 0f && file.Length * SharedNetworkResourceManager.BytesToMegabytes > sizeLimit)
        {
            shell.WriteError($"File above the current size limit! It must be smaller than {sizeLimit} MB.");
            return;
        }

        var data = file.CopyToArray();

        var netManager = IoCManager.Resolve<INetManager>();
        var msg = netManager.CreateNetMessage<NetworkResourceUploadMessage>();

        msg.RelativePath = path;
        msg.Data = data;

        netManager.ClientSendMessage(msg);
    }
}
