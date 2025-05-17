using System.Net.Http;
using System.Threading.Channels;
using System.Threading.Tasks;
using Content.Server.Github.Requests;

namespace Content.Server.Github;

public sealed class GithubApiManager
{

    private ChannelWriter<IGithubRequest> ChannelWriter = default!;

    public void Initialize(ChannelWriter<IGithubRequest> channelWriter)
    {
        ChannelWriter = channelWriter;
    }

    public bool TryMakeRequest(IGithubRequest request)
    {
        return ChannelWriter.TryWrite(request);
    }
}
