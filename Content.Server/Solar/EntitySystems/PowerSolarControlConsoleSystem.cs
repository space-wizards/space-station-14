using Content.Server.Solar.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Solar.EntitySystems
{
    /// <summary>
    /// Responsible for updating solar control consoles.
    /// </summary>
    [UsedImplicitly]
    internal sealed class PowerSolarControlConsoleSystem : EntitySystem
    {
        /// <summary>
        /// Timer used to avoid updating the UI state every frame (which would be overkill)
        /// </summary>
        private float _updateTimer;

        public override void Update(float frameTime)
        {
            _updateTimer += frameTime;
            if (_updateTimer >= 1)
            {
                _updateTimer -= 1;
                foreach (var component in EntityManager.EntityQuery<SolarControlConsoleComponent>(true))
                {
                    component.UpdateUIState();
                }
            }
        }
    }
}
