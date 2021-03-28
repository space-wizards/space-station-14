/*
MIT License

Copyright (c) 2019 Erin Catto

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

These tests are derived from box2d's testbed tests but done in a way as to be automated and useful for CI.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Physics
{
    [TestFixture]
    public class PhysicsTestBedTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestBoxStack()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var physicsSystem = entitySystemManager.GetEntitySystem<SharedPhysicsSystem>();
            MapId mapId;

            var columnCount = 1;
            var rowCount = 15;
            PhysicsComponent[] bodies = new PhysicsComponent[columnCount * rowCount];
            Vector2 firstPos = Vector2.Zero;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                physicsSystem.Maps[mapId].Gravity = new Vector2(0, -9.8f);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var ground = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId)).AddComponent<PhysicsComponent>();

                var horizontal = new EdgeShape(new Vector2(-20, 0), new Vector2(20, 0));
                var horizontalFixture = new Fixture(ground, horizontal)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };
                ground.AddFixture(horizontalFixture);

                var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
                var verticalFixture = new Fixture(ground, vertical)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };
                ground.AddFixture(verticalFixture);

                var xs = new[]
                {
                    0.0f, -10.0f, -5.0f, 5.0f, 10.0f
                };

                PolygonShape shape;

                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < rowCount; i++)
                    {
                        var x = 0.0f;

                        var box = entityManager.SpawnEntity(null,
                            new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId)).AddComponent<PhysicsComponent>();

                        box.BodyType = BodyType.Dynamic;
                        box.SleepingAllowed = false;
                        shape = new PolygonShape(0.001f) {Vertices = new List<Vector2>()
                        {
                            new(0.5f, -0.5f),
                            new(0.5f, 0.5f),
                            new(-0.5f, 0.5f),
                            new(-0.5f, -0.5f),
                        }};
                        box.FixedRotation = true;
                        // TODO: Need to detect shape and work out if we need to use fixedrotation

                        var fixture = new Fixture(box, shape)
                        {
                            CollisionMask = 1,
                            CollisionLayer = 1,
                            Hard = true,
                        };
                        box.AddFixture(fixture);

                        bodies[j * rowCount + i] = box;
                    }
                }

                firstPos = bodies[0].Owner.Transform.WorldPosition;
            });

            await server.WaitRunTicks(1);

            // Check that gravity workin
            await server.WaitAssertion(() =>
            {
                Assert.That(firstPos != bodies[0].Owner.Transform.WorldPosition);
            });

            // Assert

            await server.WaitRunTicks(150);

            // Assert settled, none below 0, etc.
            await server.WaitAssertion(() =>
            {
                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < bodies.Length; i++)
                    {
                        var body = bodies[j * columnCount + i];
                        var worldPos = body.Owner.Transform.WorldPosition;

                        // TODO: Multi-column support but I cbf right now
                        // Can't be more exact as some level of sinking is allowed.
                        Assert.That(worldPos.EqualsApprox(new Vector2(0.0f, i + 0.5f), 0.1f), $"Expected y-value of {i + 0.5f} but found {worldPos.Y}");
                    }
                }
            });
        }

        [Test]
        public async Task TestCircleStack()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var physicsSystem = entitySystemManager.GetEntitySystem<SharedPhysicsSystem>();
            MapId mapId;

            var columnCount = 1;
            var rowCount = 15;
            PhysicsComponent[] bodies = new PhysicsComponent[columnCount * rowCount];
            Vector2 firstPos = Vector2.Zero;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                physicsSystem.Maps[mapId].Gravity = new Vector2(0, -9.8f);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var ground = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId)).AddComponent<PhysicsComponent>();

                var horizontal = new EdgeShape(new Vector2(-20, 0), new Vector2(20, 0));
                var horizontalFixture = new Fixture(ground, horizontal)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };
                ground.AddFixture(horizontalFixture);

                var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
                var verticalFixture = new Fixture(ground, vertical)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };
                ground.AddFixture(verticalFixture);

                var xs = new[]
                {
                    0.0f, -10.0f, -5.0f, 5.0f, 10.0f
                };

                PhysShapeCircle shape;

                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < rowCount; i++)
                    {
                        var x = 0.0f;

                        var circle = entityManager.SpawnEntity(null,
                            new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId)).AddComponent<PhysicsComponent>();

                        circle.BodyType = BodyType.Dynamic;
                        circle.SleepingAllowed = false;
                        shape = new PhysShapeCircle {Radius = 0.5f};

                        var fixture = new Fixture(circle, shape)
                        {
                            CollisionMask = 1,
                            CollisionLayer = 1,
                            Hard = true,
                        };
                        circle.AddFixture(fixture);

                        bodies[j * rowCount + i] = circle;
                    }
                }

                firstPos = bodies[0].Owner.Transform.WorldPosition;
            });

            await server.WaitRunTicks(1);

            // Check that gravity workin
            await server.WaitAssertion(() =>
            {
                Assert.That(firstPos != bodies[0].Owner.Transform.WorldPosition);
            });

            // Assert

            await server.WaitRunTicks(150);

            // Assert settled, none below 0, etc.
            await server.WaitAssertion(() =>
            {
                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < bodies.Length; i++)
                    {
                        var body = bodies[j * columnCount + i];
                        var worldPos = body.Owner.Transform.WorldPosition;

                        // TODO: Multi-column support but I cbf right now
                        // Can't be more exact as some level of sinking is allowed.
                        Assert.That(worldPos.EqualsApprox(new Vector2(0.0f, i + 0.5f), 0.1f), $"Expected y-value of {i + 0.5f} but found {worldPos.Y}");
                    }
                }
            });
        }
    }
}
