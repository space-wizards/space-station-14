using System.Buffers.Binary;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Connection;

/// <summary>
/// Handles checking/warning if the connecting IP address is sus.
/// </summary>
public sealed partial class ConnectionManager
{
    private void InitializeIPIntel()
    {
        _cfg.OnValueChanged(CCVars.GameIPIntelEmail, b => _contactEmail = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelBase, b => _baseUrl = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelFlags, b => _flags = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelRejectUnknown, b => _rejectUnknown = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelRejectBad, b => _rejectBad = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelRejectRateLimited, b => _rejectLimited = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelMaxMinute, b => _requestLimitMinute = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelMaxDay, b => _requestLimitDay = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelBadRating, b => _rating = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelCacheLength, b => _cacheDays = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelExemptPlaytime, b => _exemptPlaytime = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelAlertAdminReject, b => _alertAdminReject = b, true);
        _cfg.OnValueChanged(CCVars.GameIPIntelAlertAdminWarnRating, b => _alertAdminWarn = b, true);
    }

    [Dependency] private readonly IHttpClientHolder _http = default!;

    private bool _ratelimitedMinute;
    private bool _ratelimitedDay;
    private int _currentRequestsDay;
    private int _currentRequestsMinute;
    private DateTime _lastRatelimited;
    private bool _limitHasBeenHandled;

    // CCVars, they are initialized in ConnectionManager.cs
    private string? _contactEmail;
    private string? _baseUrl;
    private string? _flags;
    private bool _rejectUnknown;
    private bool _rejectBad;
    private bool _rejectLimited;
    private bool _alertAdminReject;
    private int _requestLimitMinute;
    private int _requestLimitDay;
    private TimeSpan _cacheDays;
    private TimeSpan _exemptPlaytime;
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
        if (_exemptPlaytime != TimeSpan.Zero)
        {
            var overallTime = ( await _db.GetPlayTimes(e.UserId)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
            if (overallTime != null && overallTime.TimeSpent >= _exemptPlaytime)
            {
                return (false, string.Empty);
            }
        }

        var ip = e.IP.Address;

        if (IsAddressReservedIpv4(ip) || IsAddressReservedIpv6(ip))
        {
            _sawmill.Warning($"{e.UserName} joined using a local address. Do you need IPIntel? Or is something terribly misconfigured on your server?" +
                             $" Trusting this connection.");
            return (false, string.Empty);
        }

        // Check our cache
        var query = await _db.GetIPIntelCache(ip);
        var expired = false;

        // Does it exist?
        if (query != null)
        {
            // Skip to score check if result is older than _cacheDays
            if (DateTime.Now - query.Time < _cacheDays)
            {
                var cachedScore = query.Score.ToString(CultureInfo.CurrentCulture);
                return await ScoreCheck(cachedScore, e);
            }
            // This record is expired and should be updated.
            expired = true;
        }

        // Check our api limits. If we are ratelimited we back out.
        HandleRatelimit();

        if (_ratelimitedMinute || _ratelimitedDay)
            return _rejectLimited ? (true, Loc.GetString("ipintel-server-ratelimited")) : (false, string.Empty);

        // Ensure our contact email is good to use.
        if (string.IsNullOrEmpty(_contactEmail) || !_contactEmail.Contains('@') || !_contactEmail.Contains('.'))
        {
            _sawmill.Error("IPIntel is enabled, but contact email is empty or not a valid email, treating this connection like an unknown IPIntel response.");
            return _rejectUnknown ? (true, Loc.GetString("generic-misconfigured")) : (false, string.Empty);
        }

        _currentRequestsDay++;
        _currentRequestsMinute++;
        // Info about flag B: https://getipintel.net/free-proxy-vpn-tor-detection-api/#flagsb
        // TLDR: We don't care about knowing if a connection is compromised.
        // We just want to know if it's a vpn. This also speeds up the request by quite a bit. (A full scan can take 200ms to 5 seconds. This will take at most 120ms)
        using var request = await _http.Client.GetAsync($"{_baseUrl}/check.php?ip={ip}&contact={_contactEmail}&flags={_flags}");

        if (request.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _sawmill.Warning("We hit the IPIntel request limit at some point.");
            return _rejectLimited ? (true, Loc.GetString("ipintel-server-ratelimited")) : (false, string.Empty);
        }

        var response = await request.Content.ReadAsStringAsync();

        if (request.StatusCode != HttpStatusCode.BadRequest)
            return await ScoreCheck(response, e);

        // Something went wrong! Let's see if it's an error we know about
        if (ErrorCheck(response, out var rejectResult))
            return rejectResult;

        // Oh boy, we don't know this error.
        _sawmill.Error($"IPIntel returned {response} (Status code: {request.StatusCode})... we don't know what this error code is. Please make an issue in upstream!");
        return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
    }

    private bool ErrorCheck(string response, out (bool, string) result)
    {
        result = default;

        var errorMessages = new Dictionary<string, string>
        {
            ["-1"] = "Invalid/No input.",
            ["-2"] = "Invalid IP address.",
            ["-3"] = "Unroutable address / private address given to the api. Make an issue in upstream as it should have been handled.",
            ["-4"] = "Unable to reach IPIntel database. Perhaps it's down?",
            ["-5"] = "Server's IP/Contact may have been banned, go to getipintel.net and make contact to be unbanned.",
            ["-6"] = "You did not provide any contact information with your query or the contact information is invalid.",
        };

        if (!errorMessages.TryGetValue(response, out var errorMessage))
            return false;

        _sawmill.Error($"IPIntel returned error {response}: {errorMessage}");
        result = _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);

        return true;
    }

    private void HandleRatelimit()
    {
        // Oh my god this is terrible
        if (_currentRequestsDay < _requestLimitDay)
        {
            if (ShouldLiftRateLimit(_ratelimitedDay, _lastRatelimited, TimeSpan.FromDays(1)))
            {
                _sawmill.Info("IPIntel daily rate limit lifted. We are back to normal.");
                _ratelimitedDay = false;
                _currentRequestsDay = 0;
                _limitHasBeenHandled = false;
                return;
            }

            if (_limitHasBeenHandled)
                return;

            _sawmill.Warning($"We just hit our last daily IPIntel limit ({_requestLimitDay})");
            _ratelimitedDay = true;
            _lastRatelimited = DateTime.Now;
        }
        else if (_currentRequestsMinute < _requestLimitMinute)
        {
            if (ShouldLiftRateLimit(_ratelimitedMinute, _lastRatelimited, TimeSpan.FromMinutes(1)))
            {
                _sawmill.Info("IPIntel minute rate limit lifted. We are back to normal.");
                _ratelimitedMinute = false;
                _currentRequestsMinute = 0;
                _limitHasBeenHandled = false;
                return;
            }

            if (_limitHasBeenHandled)
                return;

            _sawmill.Warning($"We just hit our last minute IPIntel limit ({_requestLimitMinute}).");
            _ratelimitedMinute = true;
            _lastRatelimited = DateTime.Now;
        }
    }

    private bool ShouldLiftRateLimit(bool currentlyRatelimited, DateTime lastRatelimited, TimeSpan liftingTime)
    {
        // Should we raise this limit now?
        return currentlyRatelimited && DateTime.Now - lastRatelimited >= liftingTime;
    }

    //     await _db.UpsertIPIntelCache(DateTime.Now, ip, score);
    private async Task<(bool, string Empty)> ScoreCheck(string response, NetConnectingArgs e)
    {
        var score = Parse.Float(response);
        var ip = e.IP.Address;
        var decisionIsReject = score > _rating;

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

    // Stolen from Lidgren.Network (Space Wizards Edition) (NetReservedAddress.cs)
    // Modified with IPV6 on top
    private static int Ipv4(byte a, byte b, byte c, byte d) => (a << 24) | (b << 16) | (c << 8) | d;

    // From miniupnpc
    private static readonly (int ip, int mask)[] ReservedRangesIpv4 =
    [
        // @formatter:off
		(Ipv4(0,   0,   0,   0), 8 ), // RFC1122 "This host on this network"
		(Ipv4(10,  0,   0,   0), 8 ), // RFC1918 Private-Use
		(Ipv4(100, 64,  0,   0), 10), // RFC6598 Shared Address Space
		(Ipv4(127, 0,   0,   0), 8 ), // RFC1122 Loopback
		(Ipv4(169, 254, 0,   0), 16), // RFC3927 Link-Local
		(Ipv4(172, 16,  0,   0), 12), // RFC1918 Private-Use
		(Ipv4(192, 0,   0,   0), 24), // RFC6890 IETF Protocol Assignments
		(Ipv4(192, 0,   2,   0), 24), // RFC5737 Documentation (TEST-NET-1)
		(Ipv4(192, 31,  196, 0), 24), // RFC7535 AS112-v4
		(Ipv4(192, 52,  193, 0), 24), // RFC7450 AMT
		(Ipv4(192, 88,  99,  0), 24), // RFC7526 6to4 Relay Anycast
		(Ipv4(192, 168, 0,   0), 16), // RFC1918 Private-Use
		(Ipv4(192, 175, 48,  0), 24), // RFC7534 Direct Delegation AS112 Service
		(Ipv4(198, 18,  0,   0), 15), // RFC2544 Benchmarking
		(Ipv4(198, 51,  100, 0), 24), // RFC5737 Documentation (TEST-NET-2)
		(Ipv4(203, 0,   113, 0), 24), // RFC5737 Documentation (TEST-NET-3)
		(Ipv4(224, 0,   0,   0), 4 ), // RFC1112 Multicast
		(Ipv4(240, 0,   0,   0), 4 ), // RFC1112 Reserved for Future Use + RFC919 Limited Broadcast
        // @formatter:on
    ];

    private static readonly (UInt128 ip, int mask)[] ReservedRangesIpv6 =
    [
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("::1").GetAddressBytes()), 128), // "This host on this network"
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("::ffff:0:0").GetAddressBytes()), 96), // IPv4-mapped addresses
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("::ffff:0:0:0").GetAddressBytes()), 96), // IPv4-translated addresses
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("64:ff9b:1::").GetAddressBytes()), 48), // IPv4/IPv6 translation
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("100::").GetAddressBytes()), 64), // Discard prefix
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("2001:20::").GetAddressBytes()), 28), // ORCHIDv2
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("2001:db8::").GetAddressBytes()), 32), // Addresses used in documentation and example source code
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("3fff::").GetAddressBytes()), 20), // Addresses used in documentation and example source code
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("5f00::").GetAddressBytes()), 16), // IPv6 Segment Routing (SRv6)
        (BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse("fc00::").GetAddressBytes()), 7), // Unique local address
    ];

    private static bool IsAddressReservedIpv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        Span<byte> ipBitsByte = stackalloc byte[4];
        address.TryWriteBytes(ipBitsByte, out _);
        var ipBits = BinaryPrimitives.ReadInt32BigEndian(ipBitsByte);

        foreach (var (reservedIp, maskBits) in ReservedRangesIpv4)
        {
            var mask = uint.MaxValue << (32 - maskBits);
            if ((ipBits & mask) == (reservedIp & mask))
                return true;
        }

        return false;
    }

    private static bool IsAddressReservedIpv6(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return false;

        Span<byte> ipBitsByte = stackalloc byte[16];
        address.TryWriteBytes(ipBitsByte, out _);
        var ipBits = BinaryPrimitives.ReadInt128BigEndian(ipBitsByte);

        foreach (var (reservedIp, maskBits) in ReservedRangesIpv6)
        {
            var mask = UInt128.MaxValue << (128 - maskBits);
            if (((UInt128) ipBits & mask ) == (reservedIp & mask))
                return true;
        }

        return false;
    }
}
