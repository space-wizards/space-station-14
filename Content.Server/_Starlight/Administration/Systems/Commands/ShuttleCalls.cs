using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class AllowShuttleCallsCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IResourceManager _res = default!;

        public override string Command => "shuttlecalls";
        public override string Description => "Enable, disable, toggle, or check the status of emergency shuttle calls.";
        public override string Help =>
            "Usage: allowshuttlecalls <true|false|toggle|status> [announce] [message] [sender] [color] [sound]\n" +
            "\n" +
            "Arguments:\n" +
            "  <true|false>   Enables or disables emergency shuttle calls.\n" +
            "  toggle         Switches the current state (enabled/disabled).\n" +
            "  status         Shows whether shuttle calls are currently enabled or disabled.\n" +
            "  [announce]     (Optional) true/false) Whether to announce the change. Defaults to true.\n" +
            "  [message]      (Optional) Custom announcement message. If omitted, a default message is used.\n" +
            "  [sender]       (Optional) Announcement sender. Defaults to \"Central Command\".\n" +
            "  [color]        (Optional) Announcement color hex (e.g. #FFD700). Defaults to gold.\n" +
            "  [sound]        (Optional) Announcement sound file path.";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var roundEndSystem = EntitySystem.Get<RoundEndSystem>();

            if (args.Length == 0)
            {
                PrintStatus(shell, roundEndSystem.GetShuttleCallsEnabled());
                shell.WriteLine(Help);
                return;
            }

            var arg0 = args[0].ToLowerInvariant();

            // Status
            if (arg0 == "status")
            {
                PrintStatus(shell, roundEndSystem.GetShuttleCallsEnabled());
                return;
            }

            // Toggle
            if (arg0 == "toggle")
            {
                var newState = !roundEndSystem.GetShuttleCallsEnabled();
                roundEndSystem.SetShuttleCallsEnabled(newState);
                PrintStatus(shell, newState);

                var makeAnnouncement = true;
                if (args.Length >= 2 && bool.TryParse(args[1], out var parsedAnnounce))
                    makeAnnouncement = parsedAnnounce;

                if (makeAnnouncement)
                    Announce(shell, args, newState);

                return;
            }

            // Set directly (true/false)
            if (bool.TryParse(arg0, out var allowed))
            {
                roundEndSystem.SetShuttleCallsEnabled(allowed);
                PrintStatus(shell, allowed);

                // Announce (default true)
                var makeAnnouncement = true;
                if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
                {
                    if (!bool.TryParse(args[1], out makeAnnouncement))
                    {
                        shell.WriteError("Optional: whether to announce the change (true/false). Defaults to true.");
                        return;
                    }
                }

                if (makeAnnouncement)
                    Announce(shell, args, allowed);

                return;
            }

            shell.WriteError("First argument must be true, false, toggle, or status.");
            PrintStatus(shell, roundEndSystem.GetShuttleCallsEnabled());
        }

        private void PrintStatus(IConsoleShell shell, bool allowed)
        {
            if (allowed)
                shell.WriteLine("Shuttle calls are currently Enabled.");
            else
                shell.WriteLine("Shuttle calls are currently Disabled.");
        }

        private void Announce(IConsoleShell shell, string[] args, bool allowed)
        {
            // Announcement text
            string announcementText = args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2])
                ? args[2]
                : allowed
                    ? "Emergency shuttle calls have been enabled."
                    : "Emergency shuttle calls have been disabled.";

            // Sender text (default to Central Command)
            string senderText = args.Length >= 4 && !string.IsNullOrWhiteSpace(args[3])
                ? args[3]
                : "Central Command";

            // Color
            Color color = Color.Gold;
            if (args.Length >= 5 && !string.IsNullOrWhiteSpace(args[4]))
            {
                try
                {
                    color = Color.FromHex(args[4]);
                }
                catch
                {
                    shell.WriteError("Invalid color hex, using default gold.");
                }
            }

            // Sound
            SoundPathSpecifier? sound = null;
            if (args.Length >= 6 && !string.IsNullOrWhiteSpace(args[5]))
            {
                sound = new SoundPathSpecifier(args[5]);
            }

            _chatSystem.DispatchGlobalAnnouncement(announcementText, senderText, true, sound, color);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHintOptions(
                    new[] { "toggle", "status", "true", "false" },
                    "First argument: true/false/toggle/status — set, toggle, or check shuttle call permission.");

            if (args.Length == 2)
                return CompletionResult.FromHint("Optional: true/false — announce the change. Defaults to true.");
            if (args.Length == 3)
                return CompletionResult.FromHint("Optional: custom announcement message. Uses default if blank.");
            if (args.Length == 4)
                return CompletionResult.FromHint("Optional: sender for the announcement. Defaults to \"Central Command\".");
            if (args.Length == 5)
                return CompletionResult.FromHint("Optional: color hex (e.g. #FFD700) for the announcement. Defaults to gold.");
            if (args.Length == 6)
                return CompletionResult.FromHintOptions(
                    CompletionHelper.AudioFilePath(args.Length >= 6 ? args[5] : string.Empty, _proto, _res),
                    "Optional: sound file path for the announcement.");
            return CompletionResult.Empty;
        }
    }
}
