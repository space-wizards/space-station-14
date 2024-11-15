using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Connection;

/// <summary>
/// Handles checking/warning if the connecting IP address is sus.
/// </summary>
public sealed partial class ConnectionManager
{
    private readonly HttpClient _httpClient = new();

    private bool _ratelimited;

    // CCVars, they are initialized in ConnectionManager.cs
    private string? _contactEmail;
    private string? _baseUrl;
    private bool _rejectUnknown;
    private bool _rejectBad;
    private bool _rejectLimited;
    private bool _alertAdminReject;
    private int _requestLimitMinute;
    private int _requestLimitDay;
    private int _cacheDays;
    private int _exemptPlaytime;
    private float _rating;
    private float _alertAdminWarn;
    private async Task<(bool IsBad, string Reason)> IsVpnOrProxy(NetConnectingArgs e)
    {
        // Check Exemption flags, let them skip if they have them.
        var flags = await _db.GetBanExemption(e.UserId);
        if ((flags & (ServerBanExemptFlags.Datacenter | ServerBanExemptFlags.BlacklistedRange)) != 0)
        {
            return (false, string.Empty);
        }

        // Check playtime, if 0 we skip this check. If player has more playtime then _exemptPlaytime is configured for then they get to skip this check.
        // Helps with saving your limited request limit.
        var overallTime = ( await _db.GetPlayTimes(e.UserId)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
        if (overallTime != null && overallTime.TimeSpent.TotalMinutes >= _exemptPlaytime && _exemptPlaytime != 0f)
        {
            return (false, string.Empty);
        }

        // Check our cache
        var ip = e.IP.Address;
        var query = await _db.GetIPIntelCache(ip);
        var expired = false;

        // Does it exist?
        if (query.Count != 0)
        {
            // Skip to score check if result is older than _cacheDays
            if (!query.Any(a => (DateTime.Now - a.Time).TotalDays > _cacheDays))
            {
                var cachedScore = query.FirstOrDefault()?.Score.ToString(CultureInfo.CurrentCulture);
                return await ScoreCheck(cachedScore!, e, false, false);
            }
            // This record is expired and should be updated.
            expired = true;
        }

        // Check our api limits. If we are ratelimited we back out.
        if (_ratelimited)
            return _rejectLimited ? (true, Loc.GetString("ipintel-server-ratelimited")) : (false, string.Empty);

        // Ensure our contact email is good to use.
        if (string.IsNullOrEmpty(_contactEmail) || !_contactEmail.Contains('@') || !_contactEmail.Contains('.'))
        {
            _sawmill.Error("IPIntel is enabled, but contact email is empty or not a valid email, treating this connection like an unknown IPIntel response.");
            return _rejectUnknown ? (true, Loc.GetString("generic-misconfigured")) : (false, string.Empty);
        }

        // Info about flag B: https://getipintel.net/free-proxy-vpn-tor-detection-api/#flagsb
        // TLDR: We don't care about knowing if a connection is compromised.
        // We just want to know if it's a vpn. This also speeds up the request by quite a bit. (A full scan can take 200ms to 5 seconds. This will take at most 120ms)
        using var request = await _httpClient.GetAsync($"{_baseUrl}/check.php?ip={ip}&contact={_contactEmail}&flags=b");

        if (request.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // TODO: Ok so nik did kinda kick start my brain that... well... this could either be a daily limit or a minute limit and i would have no idea...
            // I was gonna treat this as daily but yeah i see the issue here...
            // We are just gonna try to estimate our ratelimit
            //... maybe wait a minute send the request again and if we are still ratelimited assume we hit the daily limit and cry
            // also helps to have minute/day requests stored in the db

            _sawmill.Warning("We hit the IPIntel request limit at some point.");
            _ratelimited = true;
            return _rejectLimited ? (true, Loc.GetString("ipintel-server-ratelimited")) : (false, string.Empty);
        }

        var response = await request.Content.ReadAsStringAsync();

        switch (response)
        {
            case "-1":
            {
                _sawmill.Error("IPIntel returned error -1: Invalid/No input.");
                return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
            }
            case "-2":
            {
                _sawmill.Error("IPIntel returned error -2: Invalid IP address.");
                return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
            }
            case "-3":
            {
                _sawmill.Error("IPIntel returned error -3: Unroutable address / private address given to the api.");
                return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
            }
            case "-4":
            {
                _sawmill.Error("IPIntel returned error -4: Unable to reach IPIntel database. Perhaps it's down?");
                return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
            }
            case "-5":
            {
                _sawmill.Error("IPIntel returned error -5: Server's IP/Contact may have been banned, go to getipintel.net and make contact to be unbanned.");
                return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
            }
            case "-6":
            {
                _sawmill.Error("IPIntel returned error -6: You did not provide any contact information with your query or the contact information is invalid.");
                return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
            }
            default:
            {
                return await ScoreCheck(response, e, expired, query.Count == 0);
            }
        }
    }

    private async Task<(bool, string Empty)> ScoreCheck(string response, NetConnectingArgs e, bool expired , bool newEntry)
    {
        var score = Parse.Float(response);
        var ip = e.IP.Address;
        var decisionIsReject = score > _rating;

        if (expired)
            await _db.UpdateIPIntelCache(DateTime.Now, ip, score);
        else if (newEntry)
            await _db.AddIPIntelCache(DateTime.Now, ip, score);

        if (_alertAdminWarn != 0f && _alertAdminWarn < score && !decisionIsReject)
        {
            _chatManager.SendAdminAlert(Loc.GetString("admin-alert-ipintel-warning",
                ("player", e.UserName),
                ("percent", Math.Round(score * 100))));
        }

        if (!decisionIsReject)
            return (false, string.Empty);

        if (_alertAdminReject)
        {
            _chatManager.SendAdminAlert(Loc.GetString("admin-alert-ipintel-blocked",
                ("player", e.UserName),
                ("percent", Math.Round(score * 100))));
        }

        return _rejectBad ? (true, Loc.GetString("ipintel-suspicious")) : (false, string.Empty);
    }
}
