#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems.DoAfter
{
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in ComponentManager.EntityQuery<DoAfterComponent>())
            {
                if (_pauseManager.IsGridPaused(comp.Owner.Transform.GridID)) continue;

                var cancelled = new List<DoAfter>(0);
                var finished = new List<DoAfter>(0);

                foreach (var doAfter in comp.DoAfters.ToArray())
                {
                    doAfter.Run(frameTime);

                    switch (doAfter.Status)
                    {
                        case DoAfterStatus.Running:
                            break;
                        case DoAfterStatus.Cancelled:
                            cancelled.Add(doAfter);
                            break;
                        case DoAfterStatus.Finished:
                            finished.Add(doAfter);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var doAfter in cancelled)
                {
                    comp.Cancelled(doAfter);
                }

                foreach (var doAfter in finished)
                {
                    comp.Finished(doAfter);
                }

                finished.Clear();
            }
        }

        /// <summary>
        ///     Tasks that are delayed until the specified time has passed
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public async Task<DoAfterStatus> DoAfter(DoAfterEventArgs eventArgs)
        {
            // Setup
            var doAfter = new DoAfter(eventArgs);
            // Caller's gonna be responsible for this I guess
            var doAfterComponent = eventArgs.User.GetComponent<DoAfterComponent>();
            doAfterComponent.Add(doAfter);
            IDamageableComponent? damageableComponent = null;

            // TODO: If the component's deleted this may not get unsubscribed?
            if (eventArgs.BreakOnDamage && eventArgs.User.TryGetComponent(out damageableComponent))
            {
                damageableComponent.HealthChangedEvent += doAfter.HandleDamage;
            }

            await doAfter.AsTask;

            if (damageableComponent != null)
            {
                damageableComponent.HealthChangedEvent -= doAfter.HandleDamage;
            }

            return doAfter.Status;
        }
    }

    public enum DoAfterStatus
    {
        Running,
        Cancelled,
        Finished,
    }
}
