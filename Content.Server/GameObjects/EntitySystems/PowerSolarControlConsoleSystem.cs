using Content.Server.GameObjects.Components.Power;
using JetBrains.Annotations;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Responsible for updating solar control consoles.
    /// </summary>
    [UsedImplicitly]
    public class PowerSolarControlConsoleSystem : EntitySystem
    {
        /// <summary>
        /// Timer used to avoid updating the UI state every frame (which would be overkill)
        /// </summary>
        private float UpdateTimer = 0f;

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(SolarControlConsoleComponent));
        }

        public override void Update(float frameTime)
        {
            UpdateTimer += frameTime;
            if (UpdateTimer >= 1)
            {
                UpdateTimer = 0;
                foreach (var entity in RelevantEntities)
                {
                    entity.GetComponent<SolarControlConsoleComponent>().UpdateUIState();
                }
            }
        }
    }
}
