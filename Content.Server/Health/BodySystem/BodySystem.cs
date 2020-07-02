using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Content.Server.BodySystem;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.BodySystem;
using Content.Shared.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NFluidsynth;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.BodySystem
{
    [UsedImplicitly]
    public class BodySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(BodyManagerComponent));
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var bodyManager = entity.GetComponent<BodyManagerComponent>();
            }
        }



    }
}
