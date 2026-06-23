using System.Text.RegularExpressions;
using Content.Server.Administration.Systems;
using System.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Chat.Systems;



public sealed partial class ChatSystem
{
    [Dependency] private BwoinkSystem _bwoink = default!;
    [Dependency] private IGameTiming _timing = default!;

    private float _bannedWordAhelpCooldown = 30f;
    private readonly Dictionary<NetUserId, TimeSpan> _lastBannedWordAhelp = new();
    private bool _bannedWordAhelpEnabled;
    private Regex? _bannedWordRegex;
    private void OnBannedWordListChanged(string value)
    {
        var words = value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Regex.Escape)
            .ToArray();

        _bannedWordRegex = words.Length == 0
            ? null
            : new Regex(
                $@"(?<!\p{{L}}|\p{{N}})({string.Join("|", words)})(?!\p{{L}}|\p{{N}})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private bool IsBannedWordAhelpOnCooldown(NetUserId userId)
    {
        if (_bannedWordAhelpCooldown <= 0f)
            return false;

        var now = _timing.CurTime;
        var cooldown = TimeSpan.FromSeconds(_bannedWordAhelpCooldown);

        if (_lastBannedWordAhelp.TryGetValue(userId, out var lastAlert) &&
            now - lastAlert < cooldown)
        {
            return true;
        }

        _lastBannedWordAhelp[userId] = now;
        return false;
    }

    private void CheckBannedWords(ICommonSession? player, string originalMessage)
    {
        if (!_bannedWordAhelpEnabled || player == null || _bannedWordRegex == null)
            return;

        var match = _bannedWordRegex.Match(originalMessage);
        if (!match.Success)
            return;

        if (IsBannedWordAhelpOnCooldown(player.UserId))
            return;

        var report =
            $"Automated report: I said \"{match.Value}\". \n" +
            $"in the context: \"{originalMessage}\"";

        _bwoink.SendAutomatedPlayerAHelp(player, report);
    }
}
