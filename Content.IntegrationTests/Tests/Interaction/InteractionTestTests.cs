using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Interaction;

/// <summary>
/// Makes sure that interaction test helper methods are working as intended.
/// </summary>
public sealed class InteractionTestTests : InteractionTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/empty.yml");

    /// <summary>
    /// Tests that map loading is working correctly.
    /// </summary>
    [Test]
    public void MapLoadingTest()
    {
        // Make sure that there is only one grid.
        var grids = SEntMan.AllEntities<MapGridComponent>().ToList();
        Assert.That(grids, Has.Count.EqualTo(1), "Test map did not have exactly one grid.");
        Assert.That(grids, Does.Contain(MapData.Grid), "MapData did not contain the loaded grid.");

        // Make sure we loaded the right map.
        // This name is defined in empty.yml
        Assert.That(SEntMan.GetComponent<MetaDataComponent>(MapData.MapUid).EntityName, Is.EqualTo("Empty Debug Map"));
    }
}

