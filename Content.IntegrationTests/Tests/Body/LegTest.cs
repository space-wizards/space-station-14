using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Rotation;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(SharedBodyComponent))]
    [TestOf(typeof(BodyComponent))]
    public class LegTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanBodyAndAppearanceDummy
  id: HumanBodyAndAppearanceDummy
  components:
  - type: Appearance
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso
";

        [Test]
        public async Task RemoveLegsFallTest()
        {
            var options = new ServerContentIntegrationOption{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            AppearanceComponent appearance = null;

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                var mapId = new MapId(0);
                mapManager.CreateNewMapEntity(mapId);

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var human = entityManager.SpawnEntity("HumanBodyAndAppearanceDummy", MapCoordinates.Nullspace);

                Assert.That(human.TryGetComponent(out IBody body));
                Assert.That(human.TryGetComponent(out appearance));

                Assert.That(!appearance.TryGetData(RotationVisuals.RotationState, out RotationState _));

                var legs = body.GetPartsOfType(BodyPartType.Leg);

                foreach (var leg in legs)
                {
                    body.RemovePart(leg);
                }
            });

            await server.WaitAssertion(() =>
            {
                Assert.That(appearance.TryGetData(RotationVisuals.RotationState, out RotationState state));
                Assert.That(state, Is.EqualTo(RotationState.Horizontal));
            });
        }
    }
}
