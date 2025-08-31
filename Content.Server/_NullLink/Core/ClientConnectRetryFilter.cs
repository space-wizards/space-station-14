using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Content.Server._NullLink.Core;

internal sealed class ClientConnectRetryFilter : IClientConnectionRetryFilter
{
    private const int Delay = 3000;

    public async Task<bool> ShouldRetryConnectionAttempt(
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;
        await Task.Delay(Delay, cancellationToken);
        return true;
    }
}