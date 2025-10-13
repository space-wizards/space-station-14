#nullable enable

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    public Task<TestMapData> CreateTestMap(bool initialized = true)
        => CreateTestMap(initialized, "Plating");
}
