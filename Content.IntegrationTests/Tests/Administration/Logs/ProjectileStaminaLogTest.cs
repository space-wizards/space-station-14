using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Projectiles;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(SharedStaminaSystem))]
public sealed class ProjectileStaminaLogTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [SidedDependency(Side.Server)] private readonly IAdminLogManager _sAdminLogManager = null!;

    [Test]
    [Description("Stamina damage from a projectile should be logged against the shooter, not the projectile.")]
    public async Task StaminaLogNamesShooter()
    {
        var testMap = await Pair.CreateTestMap();
        var coords = testMap.GridCoords;

        var shooter = await SpawnAtPosition("MobHuman", coords);
        var target = await SpawnAtPosition("MobHuman", coords);
        var bolt = await SpawnAtPosition("BulletDisabler", coords);

        await Server.WaitPost(() =>
        {
            var ev = new ProjectileHitEvent(new DamageSpecifier(), target, shooter);
            SEntMan.EventBus.RaiseLocalEvent(bolt, ref ev);
        });

        var expected = $"{SToPrettyString(shooter)} caused";
        var tool = $"using {SToPrettyString(bolt)}";

        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await _sAdminLogManager.CurrentRoundLogs(new LogFilter
            {
                Types = new HashSet<LogType> { LogType.Stamina },
            });

            return logs.Any(l => l.Message.StartsWith(expected) && l.Message.EndsWith(tool));
        });
    }
}
