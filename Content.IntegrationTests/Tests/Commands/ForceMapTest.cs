#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Robust.Shared.Console;

namespace Content.IntegrationTests.Tests.Commands;

[TestFixture]
public sealed class ForceMapTest : GameTest
{
    private const string DefaultMapName = "Empty";
    private const string BadMapName = "asdf_asd-fa__sdfAsd_f"; // Hopefully no one ever names a map this...
    private const string TestMapEligibleName = "ForceMapTestEligible";
    private const string TestMapIneligibleName = "ForceMapTestIneligible";

    [TestPrototypes]
    private static readonly string TestMaps = @$"
- type: gameMap
  id: {TestMapIneligibleName}
  mapName: {TestMapIneligibleName}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 20
  maxPlayers: 80
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""

- type: gameMap
  id: {TestMapEligibleName}
  mapName: {TestMapEligibleName}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
";

    [SidedDependency(Side.Server)] private IConsoleHost _sConsoleHost = null!;
    [SidedDependency(Side.Server)] private IGameMapManager _sGameMapManager = null!;


    [Test]
    [Description("Checks that the forcemap command can set and clear the selected map")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameMap), DefaultMapName)]
    [RunOnSide(Side.Server)]
    public async Task TestForceMapCommand()
    {
        // Make sure we're set to the default map
        Assert.That(_sGameMapManager.GetSelectedMap()?.ID, Is.EqualTo(DefaultMapName),
            $"Test didn't start on expected map ({DefaultMapName})!");

        // Try changing to a map that doesn't exist
        _sConsoleHost.ExecuteCommand($"forcemap {BadMapName}");
        Assert.That(_sGameMapManager.GetSelectedMap()?.ID, Is.EqualTo(DefaultMapName),
            $"Forcemap succeeded with a map that does not exist ({BadMapName})!");

        // Try changing to a valid map
        _sConsoleHost.ExecuteCommand($"forcemap {TestMapEligibleName}");
        Assert.That(_sGameMapManager.GetSelectedMap()?.ID, Is.EqualTo(TestMapEligibleName),
            $"Forcemap failed with a valid map ({TestMapEligibleName})");

        // Try changing to a map that exists but is ineligible
        _sConsoleHost.ExecuteCommand($"forcemap {TestMapIneligibleName}");
        Assert.That(_sGameMapManager.GetSelectedMap()?.ID, Is.EqualTo(TestMapIneligibleName),
            $"Forcemap failed with valid but ineligible map ({TestMapIneligibleName})!");

        // Try clearing the force-selected map
        _sConsoleHost.ExecuteCommand("forcemap \"\"");
        Assert.That(_sGameMapManager.GetSelectedMap(), Is.Null,
            $"Running 'forcemap \"\"' did not clear the forced map!");
    }
}
