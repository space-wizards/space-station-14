using System.Numerics;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.IntegrationTests.Tests.Weapons.Guns;

public sealed class GunNetworkingTest
{
    [Test]
    public async Task DeleteShooterTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var gunSys = server.System<SharedGunSystem>();

        var mapData = await pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            // Spawn gun and bullet
            var gun = entMan.SpawnEntity(null, mapData.MapCoords);
            var bullet = entMan.SpawnEntity(null, mapData.MapCoords);

            // Shoot the bullet from the gun
            gunSys.ShootProjectile(bullet, Vector2.E, Vector2.Zero, gun);

            // Delete the gun
            entMan.DeleteEntity(gun);
        });

        // Connect a fake player to the server
        var dummySession = await server.AddDummySession();

        // Force a PVS update for the fake player
        await server.WaitAssertion(() =>
        {
            Assert.DoesNotThrow(() => server.PvsTick([dummySession]));
        });

        await server.WaitRunTicks(5);

        await pair.CleanReturnAsync();
    }
}
