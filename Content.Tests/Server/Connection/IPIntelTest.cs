using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Connection.IPIntel;
using Content.Server.Database;
using Content.Shared.CCVar;
using Moq;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.UnitTesting;

// ReSharper disable AccessToModifiedClosure

namespace Content.Tests.Server.Connection;

[TestFixture, TestOf(typeof(IPIntel))]
[Parallelizable(ParallelScope.All)]
public static class IPIntelTest
{
    private static readonly IPAddress TestIp = IPAddress.Parse("192.0.2.1");

    private static void CreateIPIntel(
        out IPIntel ipIntel,
        out IConfigurationManager cfg,
        Func<HttpResponseMessage> apiResponse,
        Func<TimeSpan> realTime = null)
    {
        var dbManager = new Mock<IServerDbManager>();
        var gameTimingMock = new Mock<IGameTiming>();
        gameTimingMock.SetupGet(gt => gt.RealTime)
            .Returns(realTime ?? (() => TimeSpan.Zero));

        var logManager = new LogManager();
        var gameTiming = gameTimingMock.Object;

        cfg = MockInterfaces.MakeConfigurationManager(gameTiming, logManager, loadCvarsFromTypes: [typeof(CCVars)]);

        ipIntel = new IPIntel(
            new FakeIPIntelApi(apiResponse),
            dbManager.Object,
            cfg,
            logManager,
            new Mock<IChatManager>().Object,
            gameTiming
        );
    }

    [Test]
    public static async Task TestSuccess()
    {
        CreateIPIntel(
            out var ipIntel,
            out _,
            RespondSuccess);

        var result = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.Multiple(() =>
        {
            Assert.That(result.Score, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(result.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Success));
        });
    }

    [Test]
    public static async Task KnownRateLimitMinuteTest()
    {
        var source = RespondSuccess;
        var time = TimeSpan.Zero;
        CreateIPIntel(
            out var ipIntel,
            out var cfg,
            () => source(),
            () => time);

        cfg.SetCVar(CCVars.GameIPIntelMaxMinute, 9);

        for (var i = 0; i < 9; i++)
        {
            var result = await ipIntel.QueryIPIntelRateLimited(TestIp);
            Assert.That(result.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Success));
        }

        source = RespondTestFailed;
        var shouldBeRateLimited = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(shouldBeRateLimited.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));

        time += TimeSpan.FromMinutes(1.5);
        source = RespondSuccess;
        var shouldSucceed = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(shouldSucceed.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Success));
    }

    [Test]
    public static async Task KnownRateLimitMinuteTimingTest()
    {
        var source = RespondSuccess;
        var time = TimeSpan.Zero;
        CreateIPIntel(
            out var ipIntel,
            out var cfg,
            () => source(),
            () => time);

        cfg.SetCVar(CCVars.GameIPIntelMaxMinute, 1);

        // First query succeeds.
        var result = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(result.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Success));

        // Second is rate limited via known limit.
        source = RespondTestFailed;
        result = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(result.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));

        // Move 30 seconds into the future, should not be enough to unratelimit.
        time += TimeSpan.FromSeconds(30);

        var shouldBeRateLimited = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(shouldBeRateLimited.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));

        // Should be available again.
        source = RespondSuccess;
        time += TimeSpan.FromSeconds(35);

        var shouldSucceed = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(shouldSucceed.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Success));
    }


    [Test]
    public static async Task SuddenRateLimitTest()
    {
        var time = TimeSpan.Zero;
        var source = RespondRateLimited;
        CreateIPIntel(
            out var ipIntel,
            out _,
            () => source(),
            () => time);

        var test = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(test.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));

        source = RespondTestFailed;
        test = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(test.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));

        // King crimson idk I didn't watch JoJo past part 2.
        time += TimeSpan.FromMinutes(2);

        source = RespondSuccess;
        test = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(test.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Success));
    }

    [Test]
    public static async Task SuddenRateLimitExponentialBackoffTest()
    {
        var time = TimeSpan.Zero;
        var source = RespondRateLimited;
        CreateIPIntel(
            out var ipIntel,
            out _,
            () => source(),
            () => time);

        IPIntel.IPIntelResult test;

        for (var i = 0; i < 5; i++)
        {
            time += TimeSpan.FromHours(1);

            test = await ipIntel.QueryIPIntelRateLimited(TestIp);
            Assert.That(test.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));
        }

        // After 5 sequential failed attempts, 1 minute should not be enough to get past the exponential backoff.
        time += TimeSpan.FromMinutes(1);

        source = RespondTestFailed;
        test = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(test.Code, Is.EqualTo(IPIntel.IPIntelResultCode.RateLimited));
    }

    [Test]
    public static async Task ErrorTest()
    {
        CreateIPIntel(
            out var ipIntel,
            out _,
            RespondError);

        var resp = await ipIntel.QueryIPIntelRateLimited(TestIp);
        Assert.That(resp.Code, Is.EqualTo(IPIntel.IPIntelResultCode.Errored));
    }

    [Test]
    [TestCase("0.0.0.0", ExpectedResult = true)]
    [TestCase("0.3.5.7", ExpectedResult = true)]
    [TestCase("127.0.0.1", ExpectedResult = true)]
    [TestCase("11.0.0.0", ExpectedResult = false)]
    [TestCase("10.0.1.0", ExpectedResult = true)]
    [TestCase("192.168.5.12", ExpectedResult = true)]
    [TestCase("192.167.0.1", ExpectedResult = false)]
    // Not an IPv4!
    [TestCase("::1", ExpectedResult = false)]
    public static bool TestIsReservedIpv4(string ipAddress)
    {
        return IPIntel.IsAddressReservedIpv4(IPAddress.Parse(ipAddress));
    }

    [Test]
    // IPv4-mapped IPv6 should use IPv4 behavior.
    [TestCase("::ffff:0.0.0.0", ExpectedResult = true)]
    [TestCase("::ffff:0.3.5.7", ExpectedResult = true)]
    [TestCase("::ffff:127.0.0.1", ExpectedResult = true)]
    [TestCase("::ffff:11.0.0.0", ExpectedResult = false)]
    [TestCase("::ffff:10.0.1.0", ExpectedResult = true)]
    [TestCase("::ffff:192.168.5.12", ExpectedResult = true)]
    [TestCase("::ffff:192.167.0.1", ExpectedResult = false)]
    // Regular IPv6 tests.
    [TestCase("::1", ExpectedResult = true)]
    [TestCase("2001:db8::01", ExpectedResult = true)]
    [TestCase("2a01:4f8:252:4425::1234", ExpectedResult = false)]
    // Not an IPv6!
    [TestCase("127.0.0.1", ExpectedResult = false)]
    public static bool TestIsReservedIpv6(string ipAddress)
    {
        return IPIntel.IsAddressReservedIpv6(IPAddress.Parse(ipAddress));
    }

    private static HttpResponseMessage RespondSuccess()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("0.5"),
        };
    }

    private static HttpResponseMessage RespondRateLimited()
    {
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
    }

    private static HttpResponseMessage RespondTestFailed()
    {
        throw new InvalidOperationException("API should not be queried at this part of the test.");
    }

    private static HttpResponseMessage RespondError()
    {
        return new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("-4"),
        };
    }
}

internal sealed class FakeIPIntelApi(Func<HttpResponseMessage> response) : IIPIntelApi
{
    public Task<HttpResponseMessage> GetIPScore(IPAddress ip)
    {
        return Task.FromResult(response());
    }
}
