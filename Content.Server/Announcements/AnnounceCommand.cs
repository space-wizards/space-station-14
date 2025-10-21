using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.Announcements;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AnnounceCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public override string Command => "announce";
    public override string Description => Loc.GetString("cmd-announce-desc");
    public override string Help => Loc.GetString("cmd-announce-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
                return;
            case > 4:
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
        }

        var message = args[0];
        var sender = Loc.GetString("cmd-announce-sender");
        var color = Color.Gold;
        var sound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");

        // Optional sender argument
        if (args.Length >= 2)
            sender = args[1];

        // Optional color argument
        if (args.Length >= 3)
        {
            try
            {
                color = Color.FromHex(args[2]);
            }
            catch
            {
                shell.WriteError(Loc.GetString("shell-invalid-color-hex"));
                return;
            }
        }

        // Optional sound argument
        if (args.Length >= 4)
            sound = new SoundPathSpecifier(args[3]);

        _chat.DispatchGlobalAnnouncement(message, sender, true, sound, color);
        shell.WriteLine(Loc.GetString("shell-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-announce-arg-message")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-announce-arg-sender")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-announce-arg-color")),
            4 => CompletionResult.FromHintOptions(
                CompletionHelper.AudioFilePath(args[3], _proto, _res),
                Loc.GetString("cmd-announce-arg-sound")
            ),
            _ => CompletionResult.Empty
        };
    }
}
