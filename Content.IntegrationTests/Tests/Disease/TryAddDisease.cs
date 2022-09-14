using System.Threading.Tasks;
using Content.Server.Disease;
using Content.Shared.Disease;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Disease;

[TestFixture]
[TestOf(typeof(DiseaseSystem))]
public sealed class DeviceNetworkTest
{
    [Test]
    public async Task AddAllDiseases()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
        var server = pairTracker.Pair.Server;
        var testMap = await PoolManager.CreateTestMap(pairTracker);
        await server.WaitPost(() =>
        {
            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            var entManager = IoCManager.Resolve<IEntityManager>();
            var entSysManager = IoCManager.Resolve<IEntitySystemManager>();
            var diseaseSystem = entSysManager.GetEntitySystem<DiseaseSystem>();

            var sickEntity = entManager.SpawnEntity("MobHuman", testMap.GridCoords);
            foreach (var diseaseProto in protoManager.EnumeratePrototypes<DiseasePrototype>())
            {
                diseaseSystem.TryAddDisease(sickEntity, diseaseProto);
            }
        });
    }
}
