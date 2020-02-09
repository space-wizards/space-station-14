using System;
using Content.Client.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class YSortSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private IEyeManager _eyeManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new PredicateEntityQuery(entity => (entity.HasComponent<YSortComponent>() && entity.HasComponent<SpriteComponent>()));
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var ysort = entity.GetComponent<YSortComponent>();
                var sprite = entity.GetComponent<SpriteComponent>();
                if(!ysort.Enabled && ysort.OldPosition != entity.Transform.GridPosition) continue;
                ysort.OldPosition = entity.Transform.GridPosition;
                sprite.RenderOrder = (uint) (_eyeManager.WorldToScreen(entity.Transform.GridPosition).Y + ysort.Offset);
            }
        }
    }
}
