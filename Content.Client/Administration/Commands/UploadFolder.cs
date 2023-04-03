using System.IO;
using System.Resources;
using System.Threading.Tasks;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Client.Utility;
using Robust.Shared.ContentPack;

namespace Content.Client.Administration.Commands;

public sealed class UploadFolder : IConsoleCommand
{
    public string Command => "uploadfolder";
    public string Description => "Uploads a folder recursively to the server contentDB.";
    public string Help => $"{Command} [folder in userdata/UploadFolder] ";

    private static readonly ResourcePath BaseUploadFolderPath = new("/UploadFolder");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cfgMan = IoCManager.Resolve<IConfigurationManager>();
        var resourceMan = IoCManager.Resolve<IResourceManager>();


        if (!cfgMan.GetCVar(CCVars.ResourceUploadingEnabled))
        {
            shell.WriteError("Network Resource Uploading is currently disabled by the server.");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError($"Wrong number of arguments! \n usage: {Command} [folder in userdata/UploadFolder] ");
            return;
        }

        if (args[0].Contains(".."))
            return; // silent bomb out if the user is trying to escape the UploadFolder dir

        var folderPath = new ResourcePath(BaseUploadFolderPath + $"/{args[0]}");

        if (!resourceMan.UserData.Exists(folderPath.ToRootedPath()))
        {
            shell.WriteError($"Folder not found in /UploadFolder");
            return; // bomb out if the folder doesnt exist in /UploadFolder
        }

        //Grab all files in specified folder and upload them
        foreach (var filename in resourceMan.UserData.Find($"{folderPath.ToRelativePath()}/").files )
        {
            await using var filestream = resourceMan.UserData.Open(filename,FileMode.Open);
            {
                var sizeLimit = cfgMan.GetCVar(CCVars.ResourceUploadingLimitMb);
                if (sizeLimit > 0f && filestream.Length * SharedNetworkResourceManager.BytesToMegabytes > sizeLimit)
                {
                    shell.WriteError($"File {filename} above the current size limit! It must be smaller than {sizeLimit} MB. skipping.");
                    return;
                }

                var data = filestream.CopyToArray();

                var netManager = IoCManager.Resolve<INetManager>();
                var msg = netManager.CreateNetMessage<NetworkResourceUploadMessage>();

                msg.RelativePath = new ResourcePath($"{filename.ToString().Remove(0,14)}");
                msg.Data = data;

                netManager.ClientSendMessage(msg);
            }
        }

    }
}
