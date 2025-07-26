using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Content.Server.Github.Requests;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Github;

public sealed class GithubBackgroundWorker
{
    [Dependency] private readonly GithubClient _client = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private readonly Channel<IGithubRequest> _channel = Channel.CreateUnbounded<IGithubRequest>();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public ChannelWriter<IGithubRequest> Writer => _channel.Writer;

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("github-ratelimit");
        _cfg.OnValueChanged(CCVars.GithubEnabled, val => Interlocked.Exchange(ref _enabled, val), true);
    }

    public async Task HandleQueue()
    {
        var token = _cts.Token;
        var reader = _channel.Reader;
        while (!token.IsCancellationRequested)
        {
            await reader.WaitToReadAsync(token);
            if (!reader.TryRead(out var request))
                continue;

            await SendRequest(request, token);
        }
    }

    // this should be called in BaseServer.Cleanup!
    public void Shutdown()
    {
        _cts.Cancel();
    }

    /// <summary>
    /// Directly send a request to the API. This does not have any rate limits checks so be careful!
    /// <b>Only use this if you have a very good reason to!</b>
    /// </summary>
    /// <param name="request">The request to make.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>The direct HTTP response from the API. If null the request could not be made.</returns>
    private async Task SendRequest<T>(T request, CancellationToken ct) where T : IGithubRequest
    {
        if (!_enabled)
        {
            _sawmill.Info("Tried to make a github api request but the api was not enabled.");
            return;
        }

        try
        {
            await _client.TryMakeRequestSafe(request, ct);
        }
        catch (Exception e)
        {
            _sawmill.Error("Github API exception: {error}", e.ToString());
        }
    }
}
