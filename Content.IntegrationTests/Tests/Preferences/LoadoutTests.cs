using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Preferences;

[TestFixture]
public sealed class LoadoutTests
{
    /// <summary>
    /// Checks that an empty loadout still spawns with default gear and not naked.
    /// </summary>
    [Test]
    public async Task TestEmptyLoadout()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings());
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();

        // Check that an empty role loadout spawns gear
        var stationSystem = entManager.System<StationSpawningSystem>();
        var testMap = await pair.CreateTestMap();

        var profile = new HumanoidCharacterProfile();

        profile.SetLoadout(new RoleLoadout("TestRoleLoadout"));

        stationSystem.SpawnPlayerMob(testMap.GridCoords, job: new JobComponent()
        {
            // Sue me, there's so much involved in setting up jobs
            Prototype = "JobCargoTechnician"
        }, profile, station: null);

        await pair.CleanReturnAsync();
    }
}
