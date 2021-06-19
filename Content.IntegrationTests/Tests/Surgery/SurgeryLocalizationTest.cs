#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Operation.Step;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Surgery
{
    [TestFixture]
    [TestOf(typeof(SurgeryOperationPrototype))]
    public class SurgeryLocalizationTest : ContentIntegrationTest
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        // ReSharper disable once RedundantEmptyObjectOrCollectionInitializer
        private static readonly HashSet<string> ExceptIds = new() {};

        private void AddIfMissing(ILocalizationManager loc, string id, List<string> missingIds, params (string, object)[] args)
        {
            if (!loc.TryGetString(id,  out _, args))
            {
                missingIds.Add(id);
            }
        }

        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sLoc = server.ResolveDependency<ILocalizationManager>();

            var missing = new List<string>();

            await server.WaitPost(() =>
            {
                var mapId = new MapId(1);
                sMapManager.CreateMap(mapId);

                var coordinates = new MapCoordinates(0, 0, new MapId(1));
                var surgeon = sEntityManager.SpawnEntity(null, coordinates);
                var target = sEntityManager.SpawnEntity(null, coordinates);
                var part = sEntityManager.SpawnEntity(null, coordinates);
                var item = sEntityManager.SpawnEntity(null, coordinates);

                foreach (var operation in sPrototypeManager.EnumeratePrototypes<SurgeryStepPrototype>())
                {
                    if (ExceptIds.Contains(operation.ID))
                    {
                        continue;
                    }

                    var id = operation.ID.ToLowerInvariant();
                    var args = new (string, object)[] {("user", surgeon), ("target", target), ("part", part), ("item", item)};

                    // - Begin
                    var beginId = $"surgery-step-{id}-begin";

                    // Surgeon
                    AddIfMissing(sLoc, $"{beginId}-surgeon-popup", missing, args);
                    AddIfMissing(sLoc, $"{beginId}-self-surgeon-popup", missing, args);
                    AddIfMissing(sLoc, $"{beginId}-no-zone-surgeon-popup", missing, args);

                    // Target
                    AddIfMissing(sLoc, $"{beginId}-target-popup", missing, args);

                    // Outsider
                    AddIfMissing(sLoc, $"{beginId}-outsider-popup", missing, args);
                    AddIfMissing(sLoc, $"{beginId}-self-outsider-popup", missing, args);
                    AddIfMissing(sLoc, $"{beginId}-no-zone-outsider-popup", missing, args);


                    // - Success
                    var successId = $"surgery-step-{id}-success";

                    // Surgeon
                    AddIfMissing(sLoc,$"{successId}-surgeon-popup", missing, args);
                    AddIfMissing(sLoc, $"{successId}-self-surgeon-popup", missing, args);
                    AddIfMissing(sLoc, $"{successId}-no-zone-surgeon-popup", missing, args);

                    // Target
                    AddIfMissing(sLoc, $"{successId}-target-popup", missing, args);

                    // Outsider
                    AddIfMissing(sLoc, $"{successId}-outsider-popup", missing, args);
                    AddIfMissing(sLoc, $"{successId}-self-outsider-popup", missing, args);
                    AddIfMissing(sLoc, $"{successId}-no-zone-outsider-popup", missing, args);
                }
            });

            if (missing.Count == 0)
            {
                Assert.Pass();
                return;
            }

            Assert.Fail($"Missing {missing.Count} lines:\n{string.Join("\n", missing)}");
        }
    }
}
