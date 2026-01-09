namespace Content.Packaging;

public sealed class SharedPackaging
{
    public static readonly IReadOnlySet<string> AdditionalIgnoredResources = new HashSet<string>
    {
        // MapRenderer outputs into Resources. Avoid these getting included in packaging.
        "MapImages",
    };
}
