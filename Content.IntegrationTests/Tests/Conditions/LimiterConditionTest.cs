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
    [TestOf(typeof(LimiterCondition))]
    public sealed class LimiterConditionTest : GameTest
    {
        [Test]
        public async Task EvaluateLimiterCondition()
        {
            var pair = Pair;
            var server = pair.Server;

         //   var testMap = await pair.CreateTestMap();

            var evaluationSystem = server.System<SharedConditionEvaluationSystem>();

            await server.WaitAssertion(() =>
            {

                var entity=SSpawn(null);

                var absoluteBottom = new AbsoluteCondition(){Value = 0};

                var absoluteMiddle= new AbsoluteCondition(){Value = 50};

                var absoluteTop = new AbsoluteCondition(){Value = 100};

                var limiterBottom = new LimiterCondition()
                {
                    Condition = absoluteBottom,
                    MaximumOutputValue = 75,
                    MinimumOutputValue = 25,
                };

                var value=evaluationSystem.EvaluateCondition(limiterBottom, entity, null);

                Assert.That(value, Is.EqualTo(25f));

                var limiterMiddle = new LimiterCondition()
                {
                    Condition = absoluteMiddle,
                    MaximumOutputValue = 75,
                    MinimumOutputValue = 25,
                };

                value=evaluationSystem.EvaluateCondition(limiterMiddle, entity, null);

                Assert.That(value, Is.EqualTo(50f));

                var limiterTop = new LimiterCondition()
                {
                    Condition = absoluteTop,
                    MaximumOutputValue = 75,
                    MinimumOutputValue = 25,
                };

                value=evaluationSystem.EvaluateCondition(limiterTop, entity, null);

                Assert.That(value, Is.EqualTo(75f));

            });
        }
    }
}
