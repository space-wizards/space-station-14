using System.Threading.Tasks;
using Content.Server.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Rotation;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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
  - type: StandingState
";

        [Test]
        public async Task RemoveLegsFallTest()
        {
            var options = new ServerContentIntegrationOption{ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            AppearanceComponent appearance = null;

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                var mapId = mapManager.CreateMap();

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var human = entityManager.SpawnEntity("HumanBodyAndAppearanceDummy", new MapCoordinates(Vector2.Zero, mapId));

                Assert.That(human.TryGetComponent(out SharedBodyComponent body));
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
