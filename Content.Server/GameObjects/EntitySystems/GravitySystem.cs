using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Gravity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GravitySystem: EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery<GravityGeneratorComponent>();
        }

        public override void Update(float frameTime)
        {
            var gridsWithGravity = new List<GridId>();
            foreach (var entity in RelevantEntities)
            {
                var generator = entity.GetComponent<GravityGeneratorComponent>();
                generator.UpdateSprite();
                if (generator.Status == GravityGeneratorStatus.On)
                {
                    gridsWithGravity.Add(entity.Transform.GridID);
                }
            }

            foreach (var grid in _mapManager.GetAllGrids())
            {
                grid.HasGravity = gridsWithGravity.Contains(grid.Index);
            }
        }
    }
}
