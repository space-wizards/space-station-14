#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        private TypeEntityQuery _entityQuery = new TypeEntityQuery(typeof(DoAfterComponent));

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            
            foreach (var entity in EntityManager.GetEntities(_entityQuery))
            {
                if (_pauseManager.IsGridPaused(entity.Transform.GridID)) continue;
                
                var comp = entity.GetComponent<DoAfterComponent>();
                var cancelled = new List<DoAfter>(0);
                var finished = new List<DoAfter>(0);

                foreach (var doAfter in comp.DoAfters)
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
            DamageableComponent? damageableComponent = null;
            
            // TODO: If the component's deleted this may not get unsubscribed?
            if (eventArgs.BreakOnDamage && eventArgs.User.TryGetComponent(out damageableComponent))
            {
                damageableComponent.Damaged += doAfter.HandleDamage;
            }

            await doAfter.AsTask;
            
            if (damageableComponent != null)
            {
                damageableComponent.Damaged -= doAfter.HandleDamage;
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