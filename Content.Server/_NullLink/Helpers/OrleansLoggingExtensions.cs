using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;

namespace Content.Server._NullLink.Helpers;

internal static class OrleansLoggingExtensions
{
    public static IClientBuilder AddRobustSawmill(this IClientBuilder builder, ISawmill saw)
    {
        builder.ConfigureServices(services =>
            services.AddLogging(lb => lb.AddProvider(new RobustSawmillProvider(saw)))
        );

        return builder;
    }
}
