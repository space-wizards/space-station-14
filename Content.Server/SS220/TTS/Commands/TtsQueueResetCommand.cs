// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.SS220.TTS.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class TtsQueueResetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    public string Command => "allttsqueuereset";
    public string Description => "Reset TTS queues on all connected clients";
    public string Help => "allttsqueuereset";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ttsSys = _entitySystemManager.GetEntitySystem<TTSSystem>();
        ttsSys.RequestResetAllClientQueues();

        _chat.DispatchServerAnnouncement(Loc.GetString("command-tts-reset-request-dispatch"));
        shell.WriteLine("TTS queue reset request has been sent.");
    }
}
