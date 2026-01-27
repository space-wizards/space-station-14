using Content.Shared.Atmos;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Class for testing some airflow retrieval API methods.
/// </summary>
public sealed class GetAirflowDirectionsTest : AtmosTest
{
    // i will keep using this test map until it has been drained
    // of all use
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    [Test]
    [TestCase(0, 0, AtmosDirection.All)]
    [TestCase(0, 1, AtmosDirection.South)]
    [TestCase(0, -1, AtmosDirection.North)]
    [TestCase(1, 0, AtmosDirection.West)]
    [TestCase(-1, 0, AtmosDirection.East)]
    [TestCase(1, 1, AtmosDirection.Invalid)]
    public async Task TestLookups(int x, int y, AtmosDirection expectedDirections)
    {
        await Server.WaitPost(delegate
        {
            // yea
            var coords = new Vector2i(x, y);
            var directions = SAtmos.GetAirflowDirections(RelevantAtmos, coords);
            Assert.That(directions, Is.EqualTo(expectedDirections));
        });
    }

}
