using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Conditions;
using Content.Shared.Coordinates;
using Content.Shared.EntityConditions.Conditions.Math;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Conditions
{
    [TestFixture]
    [TestOf(typeof(AbsoluteCondition))]
    public sealed class AbsoluteConditionTest : GameTest
    {
        [Test]
        public async Task EvaluateAbsoluteCondition()
        {
            var pair = Pair;
            var server = pair.Server;

         //   var testMap = await pair.CreateTestMap();

            var evaluationSystem = server.System<SharedConditionEvaluationSystem>();

            await server.WaitAssertion(() =>
            {

                var entity=SSpawn(null);

                var condition=new AbsoluteCondition(){Value = 555};

                var value=evaluationSystem.EvaluateCondition(condition, entity, null);

                Assert.That(value, Is.EqualTo(555));

            });
        }
    }
}
