using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Connection.IPIntel;

// Handles checking/warning if the connecting IP address is sus.
public sealed class IPIntel
{
    private readonly IIPIntelApi _api;
    private readonly IServerDbManager _db;
    private readonly IChatManager _chatManager;
    private readonly IGameTiming _gameTiming;

    private ISawmill _sawmill;

    public IPIntel(IIPIntelApi api,
        IServerDbManager db,
        IConfigurationManager cfg,
        ILogManager logManager,
        IChatManager chatManager,
        IGameTiming gameTiming)
    {
        _api = api;
        _db = db;
        _chatManager = chatManager;
        _gameTiming = gameTiming;

        _sawmill = logManager.GetSawmill("ipintel");

        cfg.OnValueChanged(CCVars.GameIPIntelEmail, b => _contactEmail = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelRejectUnknown, b => _rejectUnknown = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelRejectBad, b => _rejectBad = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelRejectRateLimited, b => _rejectLimited = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelMaxMinute, b => _requestLimitMinute = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelMaxDay, b => _requestLimitDay = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelBackOffSeconds, b => _backoffSeconds = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelCleanupMins, b => _cleanupMins = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelBadRating, b => _rating = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelCacheLength, b => _cacheDays = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelExemptPlaytime, b => _exemptPlaytime = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelAlertAdminReject, b => _alertAdminReject = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelAlertAdminWarnRating, b => _alertAdminWarn = b, true);
    }

    internal struct Ratelimits
    {
        public bool RateLimited { get; set; }
        public int CurrentRequests { get; set; }
        public TimeSpan LastRatelimited { get; set; }
    }

    private Ratelimits _day;
    /// This is internal for the reasoning of tests. Please DO NOT MODIFY IT outside the IPIntel function.
    internal Ratelimits _minute;

    private bool _limitHasBeenHandled;

    private TimeSpan _nextClean;

    private int _failedRequests;

    /// This is internal for the reasoning of tests. Please DO NOT MODIFY IT outside the IPIntel function.
    internal DateTime ReleasePeriod;

    // CCVars
    private string? _contactEmail;
    private bool _rejectUnknown;
    private bool _rejectBad;
    private bool _rejectLimited;
    private bool _alertAdminReject;
    private int _requestLimitMinute;
    private int _requestLimitDay;
    private int _backoffSeconds;
    private int _cleanupMins;
    private TimeSpan _cacheDays;
    private TimeSpan _exemptPlaytime;
    private float _rating;
    private float _alertAdminWarn;

    public async Task<(bool IsBad, string Reason)> IsVpnOrProxy(NetConnectingArgs e)
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
        var username = e.UserName;

        // Is this a local ip address?
        if (IsAddressReservedIpv4(ip) || IsAddressReservedIpv6(ip))
        {
            _sawmill.Warning($"{e.UserName} joined using a local address. Do you need IPIntel? Or is something terribly misconfigured on your server? Trusting this connection.");
            return (false, string.Empty);
        }

        // Check our cache
        var query = await _db.GetIPIntelCache(ip);

        // Does it exist?
        if (query != null)
        {
            // Skip to score check if result is older than _cacheDays
            if (DateTime.UtcNow - query.Time <= _cacheDays)
            {
                var score = query.Score;
                return ScoreCheck(score, username);
            }
        }

        // Ensure our contact email is good to use.
        if (string.IsNullOrEmpty(_contactEmail) || !_contactEmail.Contains('@') || !_contactEmail.Contains('.'))
        {
            _sawmill.Error("IPIntel is enabled, but contact email is empty or not a valid email, treating this connection like an unknown IPIntel response.");
            return _rejectUnknown ? (true, Loc.GetString("generic-misconfigured")) : (false, string.Empty);
        }

        return await QueryIPIntel(username, ip);
    }

    public async Task<(bool IsBad, string Reason)> QueryIPIntel(string username, IPAddress ip)
    {
        _day.CurrentRequests++;
        _minute.CurrentRequests++;

        // Check our api limits. If we are ratelimited we back out.
        HandleRatelimit();

        if (_minute.RateLimited || _day.RateLimited || CheckSuddenRateLimit())
            return _rejectLimited ? (true, Loc.GetString("ipintel-server-ratelimited")) : (false, string.Empty);

        // Info about flag B: https://getipintel.net/free-proxy-vpn-tor-detection-api/#flagsb
        // TLDR: We don't care about knowing if a connection is compromised.
        // We just want to know if it's a vpn. This also speeds up the request by quite a bit. (A full scan can take 200ms to 5 seconds. This will take at most 120ms)
        using var request = await _api.GetIPScore(ip);

        if (request.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _sawmill.Warning($"We hit the IPIntel request limit at some point. (Current limit count: Minute: {_minute.CurrentRequests} Day: {_day.CurrentRequests})");
            CalculateSuddenRatelimit();
            return _rejectLimited ? (true, Loc.GetString("ipintel-server-ratelimited")) : (false, string.Empty);
        }

        var response = await request.Content.ReadAsStringAsync();
        var score = Parse.Float(response);

        if (request.StatusCode == HttpStatusCode.OK)
        {
            await Task.Run(() => _db.UpsertIPIntelCache(DateTime.UtcNow, ip, score));
            return ScoreCheck(score, username);
        }

        // Something went wrong! Let's see if it's an error we know about
        if (ErrorCheck(response, out var rejectResult))
            return rejectResult;

        // Oh boy, we don't know this error.
        _sawmill.Error($"IPIntel returned {response} (Status code: {request.StatusCode})... we don't know what this error code is. Please make an issue in upstream!");
        return _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);
    }



    private bool CheckSuddenRateLimit()
    {
        if (_failedRequests >= 1)
        {
            if (ReleasePeriod < DateTime.UtcNow)
            {
                CalculateSuddenRatelimit();
                return true;
            }
        }

        _failedRequests = 0;
        return false;
    }

    private void CalculateSuddenRatelimit()
    {
        _failedRequests++;
        ReleasePeriod = DateTime.UtcNow + TimeSpan.FromSeconds(_failedRequests * _backoffSeconds);
    }

    private static readonly Dictionary<string, string> ErrorMessages = new()
    {
        ["-1"] = "Invalid/No input.",
        ["-2"] = "Invalid IP address.",
        ["-3"] = "Unroutable address / private address given to the api. Make an issue in upstream as it should have been handled.",
        ["-4"] = "Unable to reach IPIntel database. Perhaps it's down?",
        ["-5"] = "Server's IP/Contact may have been banned, go to getipintel.net and make contact to be unbanned.",
        ["-6"] = "You did not provide any contact information with your query or the contact information is invalid.",
    };

    private bool ErrorCheck(string response, out (bool, string) result)
    {
        result = default;

        if (!ErrorMessages.TryGetValue(response, out var errorMessage))
            return false;

        _sawmill.Error($"IPIntel returned error {response}: {errorMessage}");
        result = _rejectUnknown ? (true, Loc.GetString("ipintel-unknown")) : (false, string.Empty);

        return true;
    }

    private void HandleRatelimit()
    {
        // Oh my god this is terrible
        if (_day.CurrentRequests >= _requestLimitDay)
        {
            if (ShouldLiftRateLimit(_day.RateLimited, _day.LastRatelimited, TimeSpan.FromDays(1)))
            {
                _sawmill.Info("IPIntel daily rate limit lifted. We are back to normal.");
                _day.RateLimited = false;
                _day.CurrentRequests = 0;
                _limitHasBeenHandled = false;
                return;
            }

            if (_limitHasBeenHandled)
                return;

            _sawmill.Warning($"We just hit our last daily IPIntel limit ({_requestLimitDay})");
            _day.RateLimited = true;
            _limitHasBeenHandled = true;
            _day.LastRatelimited = _gameTiming.RealTime;
        }
        else if (_minute.CurrentRequests >= _requestLimitMinute)
        {
            if (ShouldLiftRateLimit(_minute.RateLimited, _minute.LastRatelimited, TimeSpan.FromMinutes(1)))
            {
                _sawmill.Info("IPIntel minute rate limit lifted. We are back to normal.");
                _minute.RateLimited = false;
                _minute.CurrentRequests = 0;
                _limitHasBeenHandled = false;
                return;
            }

            if (_limitHasBeenHandled)
                return;

            _sawmill.Warning($"We just hit our last minute IPIntel limit ({_requestLimitMinute}).");
            _minute.RateLimited = true;
            _limitHasBeenHandled = true;
            _minute.LastRatelimited = _gameTiming.RealTime;
        }
    }

    private bool ShouldLiftRateLimit(bool currentlyRatelimited, TimeSpan lastRatelimited, TimeSpan liftingTime)
    {
        // Should we raise this limit now?
        return currentlyRatelimited && _gameTiming.RealTime >= lastRatelimited + liftingTime;
    }

    private (bool, string Empty) ScoreCheck(float score, string username)
    {
        var decisionIsReject = score > _rating;

        if (_alertAdminWarn != 0f && _alertAdminWarn < score && !decisionIsReject)
        {
            _chatManager.SendAdminAlert(Loc.GetString("admin-alert-ipintel-warning",
                ("player", username),
                ("percent", Math.Round(score))));
        }

        if (!decisionIsReject)
            return (false, string.Empty);

        if (_alertAdminReject)
        {
            _chatManager.SendAdminAlert(Loc.GetString("admin-alert-ipintel-blocked",
                ("player", username),
                ("percent", Math.Round(score))));
        }

        return _rejectBad ? (true, Loc.GetString("ipintel-suspicious")) : (false, string.Empty);
    }

    public void Update()
    {
        if (_gameTiming.RealTime >= _nextClean)
        {
            _nextClean = _gameTiming.RealTime + TimeSpan.FromMinutes(_cleanupMins);
            _db.CleanIPIntelCache(_cacheDays);
        }
    }

    // Stolen from Lidgren.Network (Space Wizards Edition) (NetReservedAddress.cs)
    // Modified with IPV6 on top
    private static int Ipv4(byte a, byte b, byte c, byte d)
    {
        return (a << 24) | (b << 16) | (c << 8) | d;
    }

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

    private static UInt128 ToAddressBytes(string ip)
    {
        return BinaryPrimitives.ReadUInt128BigEndian(IPAddress.Parse(ip).GetAddressBytes());
    }

    private static readonly (UInt128 ip, int mask)[] ReservedRangesIpv6 =
    [
        (ToAddressBytes("::1"), 128), // "This host on this network"
        (ToAddressBytes("::ffff:0:0"), 96), // IPv4-mapped addresses
        (ToAddressBytes("::ffff:0:0:0"), 96), // IPv4-translated addresses
        (ToAddressBytes("64:ff9b:1::"), 48), // IPv4/IPv6 translation
        (ToAddressBytes("100::"), 64), // Discard prefix
        (ToAddressBytes("2001:20::"), 28), // ORCHIDv2
        (ToAddressBytes("2001:db8::"), 32), // Addresses used in documentation and example source code
        (ToAddressBytes("3fff::"), 20), // Addresses used in documentation and example source code
        (ToAddressBytes("5f00::"), 16), // IPv6 Segment Routing (SRv6)
        (ToAddressBytes("fc00::"), 7), // Unique local address
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
