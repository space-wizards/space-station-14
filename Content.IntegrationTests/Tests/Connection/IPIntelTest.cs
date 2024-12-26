using System.Net;
using System.Net.Http;
using Content.Server.Chat.Managers;
using Content.Server.Connection.IPIntel;
using Content.Server.Database;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Connection;

[TestFixture]
public sealed class IPIntelTest
{
    private static RobustIntegrationTest.ServerIntegrationInstance _server;
    private static IConfigurationManager _cfg = null!;

    [OneTimeSetUp]
    public static async Task Setup()
    {
        var server = (await PoolManager.GetServerClient()).Server;
        var cfg = server.CfgMan;

        _server = server;
        _cfg = cfg;
    }

    private IPIntel CreateIPIntel(HttpResponseMessage response)
    {
        var db = _server.ResolveDependency<IServerDbManager>();
        var cfg = _server.CfgMan;
        var log = _server.ResolveDependency<ILogManager>();
        var chat = _server.ResolveDependency<IChatManager>();
        var timing = _server.ResolveDependency<IGameTiming>();

        return new IPIntel(new FakeIPIntelApi(response), db, cfg, log, chat, timing);
    }

    [Test]
    public async Task KnownRateLimitTest()
    {
        await _server.WaitAssertion(async void () =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("0.5"),
            };

            var ipintel = CreateIPIntel(response);

            _cfg.SetCVar(CCVars.GameIPIntelRejectRateLimited, true);
            _cfg.SetCVar(CCVars.GameIPIntelMaxMinute, 9);

            for (var i = 0; i < 9; i++)
            {
                await ipintel.QueryIPIntel("UristMcFoobar", IPAddress.Parse("1.2.3.4"));
            }

            var shouldFail = await ipintel.QueryIPIntel("UristMcBarFoo", IPAddress.Parse("1.2.3.4"));

            Assert.That(shouldFail.IsBad);
        });
    }

    [Test]
    public async Task SuddenRateLimitTest()
    {
        await _server.WaitAssertion(async void () =>
            {
                var ipintel = CreateIPIntel(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

                _cfg.SetCVar(CCVars.GameIPIntelRejectRateLimited, true);

                var test = await ipintel.QueryIPIntel("UristMcFoobar", IPAddress.Parse("1.2.3.4"));
                Assert.That(test.IsBad);
            }
        );
    }

    [Test]
    public async Task KnownRateLimitLiftTest()
    {
        await _server.WaitAssertion(async void () =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("0.5"),
            };

            var ipintel = CreateIPIntel(response);

            _cfg.SetCVar(CCVars.GameIPIntelRejectRateLimited, true);
            _cfg.SetCVar(CCVars.GameIPIntelMaxMinute, 9);


            for (var i = 0; i < 9; i++)
            {
                await ipintel.QueryIPIntel("UristMcFoobar", IPAddress.Parse("1.2.3.4"));
            }

            // Simulate time passing by removing 2 minutes from the LastRatelimited time
            ipintel._minute.LastRatelimited -= TimeSpan.FromMinutes(2);

            var shouldSucceed = await ipintel.QueryIPIntel("UristMcBarFoo", IPAddress.Parse("1.2.3.4"));

            Assert.That(!shouldSucceed.IsBad);
        });
    }

    [Test]
    public async Task SuddenRateLimitLiftTest()
    {
        await _server.WaitAssertion(async void () =>
            {
                var ipintel = CreateIPIntel(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

                _cfg.SetCVar(CCVars.GameIPIntelRejectRateLimited, true);

                await ipintel.QueryIPIntel("UristMcFoobar", IPAddress.Parse("1.2.3.4"));

                // Pretend we are no longer rate limited
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("0.5"),
                };

                ipintel = CreateIPIntel(response);

                // Use king crimson to go 2 mins into the future, the ratelimit is probably for 30 seconds.
                ipintel.ReleasePeriod += TimeSpan.FromMinutes(2);

                var test = await ipintel.QueryIPIntel("UristMcFoobar", IPAddress.Parse("1.2.3.4"));

                Assert.That(!test.IsBad);
            }
        );
    }
}

public sealed class FakeIPIntelApi(HttpResponseMessage response) : IIPIntelApi
{
    public Task<HttpResponseMessage> GetIPScore(IPAddress ip)
    {
        return Task.FromResult(response);
    }
}
