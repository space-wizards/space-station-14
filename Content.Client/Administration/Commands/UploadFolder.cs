using System.IO;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Commands;

public sealed class UploadFolder : IConsoleCommand
{
    public string Command => "uploadfolder";
    public string Description => Loc.GetString("uploadfolder-command-description");
    public string Help => Loc.GetString("uploadfolder-command-help");

    private static readonly ResourcePath BaseUploadFolderPath = new("/UploadFolder");

    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var fileCount = 0;


        if (!_configManager.GetCVar(CCVars.ResourceUploadingEnabled))
        {
            shell.WriteError( Loc.GetString("uploadfolder-command-resource-upload-disabled"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError( Loc.GetString("uploadfolder-command-wrong-args"));
            shell.WriteLine( Loc.GetString("uploadfolder-command-help"));
            return;
        }
        var folderPath = new ResourcePath(BaseUploadFolderPath + $"/{args[0]}");

        if (!_resourceManager.UserData.Exists(folderPath.ToRootedPath()))
        {
            shell.WriteError( Loc.GetString("uploadfolder-command-folder-not-found",("folder", folderPath)));
            return; // bomb out if the folder doesnt exist in /UploadFolder
        }

        //Grab all files in specified folder and upload them
        foreach (var filepath in _resourceManager.UserData.Find($"{folderPath.ToRelativePath()}/").files )
        {

            await using var filestream = _resourceManager.UserData.Open(filepath,FileMode.Open);
            {
                var sizeLimit = _configManager.GetCVar(CCVars.ResourceUploadingLimitMb);
                if (sizeLimit > 0f && filestream.Length * SharedNetworkResourceManager.BytesToMegabytes > sizeLimit)
                {
                    shell.WriteError( Loc.GetString("uploadfolder-command-file-too-big", ("filename",filepath), ("sizeLimit",sizeLimit)));
                    return;
                }

                var data = filestream.CopyToArray();

                var netManager = IoCManager.Resolve<INetManager>();
                var msg = netManager.CreateNetMessage<NetworkResourceUploadMessage>();

                msg.RelativePath = new ResourcePath($"{filepath.ToString().Remove(0,14)}"); //removes /UploadFolder/ from path
                msg.Data = data;

                netManager.ClientSendMessage(msg);
                fileCount++;
            }
        }

        shell.WriteLine( Loc.GetString("uploadfolder-command-success",("fileCount",fileCount)));
    }
}
