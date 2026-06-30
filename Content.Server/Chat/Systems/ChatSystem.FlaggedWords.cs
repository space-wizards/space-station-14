using System.Text.RegularExpressions;
using Content.Server.Administration.Systems;
using System.Linq;
using System.Text;
using Content.Shared.CCVar;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Chat.Systems;
public sealed partial class ChatSystem
{
    [Dependency] private BwoinkSystem _bwoink = default!;
    [Dependency] private IGameTiming _timing = default!;

    private float _FlaggedWordAhelpCooldown = 30f;
    private readonly Dictionary<NetUserId, TimeSpan> _lastFlaggedWordAhelp = new();
    private bool _FlaggedWordAhelpEnabled;
    private Regex? _FlaggedWordRegex;
    private void OnFlaggedWordListChanged(string value)
    {
        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            // Invalid base64, fail gracefully?
            _FlaggedWordRegex = null;
            return;
        }

        var words = decoded
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Regex.Escape)
            .ToArray();

        _FlaggedWordRegex = words.Length == 0
            ? null
            : new Regex(
                $@"(?<!\p{{L}}|\p{{N}})({string.Join("|", words)})(?!\p{{L}}|\p{{N}})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private bool IsFlaggedWordAhelpOnCooldown(NetUserId userId)
    {
        if (_FlaggedWordAhelpCooldown <= 0f)
            return false;

        var now = _timing.CurTime;
        var cooldown = TimeSpan.FromSeconds(_FlaggedWordAhelpCooldown);

        if (_lastFlaggedWordAhelp.TryGetValue(userId, out var lastAlert) &&
            now - lastAlert < cooldown)
        {
            return true;
        }

        _lastFlaggedWordAhelp[userId] = now;
        return false;
    }

    private void CheckFlaggedWords(ICommonSession? player, string originalMessage)
    {
        if (!_FlaggedWordAhelpEnabled || player == null || _FlaggedWordRegex == null)
            return;

        var match = _FlaggedWordRegex.Match(originalMessage);
        if (!match.Success)
            return;

        if (IsFlaggedWordAhelpOnCooldown(player.UserId))
            return;

        var report = Loc.GetString("admin-flagged-word", ("flagged", match.Value), ("original", originalMessage));

        _bwoink.SendAutomatedPlayerAHelp(player, report);
    }
}
