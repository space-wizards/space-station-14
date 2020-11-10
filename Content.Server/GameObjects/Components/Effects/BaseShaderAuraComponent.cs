using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Effects
{

    /// <summary>
    ///      MUST BE IMPLEMENTED. If a player is in range, calls the OnEnterRange function with a reference to its <see cref="ServerOverlayEffectsComponent"/>. Similarly, calls
    ///      OnExitRange on exit. Useful for permanent effects, like a singularity warping space around it.
    /// </summary>
    [RegisterComponent]
    public class BaseShaderAuraComponent : Component
    {
        public override string Name => "ShaderAura";

        [Dependency] protected readonly IPlayerManager PlayerManager = default!;
        [Dependency] protected readonly IEntityManager EntityManager = default!;

        protected List<IEntity> ActivatedEntities = new List<IEntity>();
        protected virtual int Radius => 20;

        public override void OnRemove()
        {
            base.OnRemove();
            foreach (var entity in ActivatedEntities)
            {
                if (entity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                {
                    OnExitRange(entity, overlayEffects);
                }
            }
        }

        public void OnTick()
        {
            List<IEntity> entities = EntityManager.GetEntitiesInRange(Owner.Transform.Coordinates, Radius).ToList();
            foreach (var entity in entities) {
                if (!ActivatedEntities.Contains(entity))
                { 
                    if (entity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                    {
                        ActivatedEntities.Add(entity);
                        OnEnterRange(entity, overlayEffects);
                    }
                }
            }
            for (int i = 0; i < ActivatedEntities.Count; i++)
            {
                var entity = ActivatedEntities[i];
                if (!entities.Contains(entity))
                {
                    ActivatedEntities.Remove(entity);
                    i--;
                    if (entity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                    {
                        OnExitRange(entity, overlayEffects);
                    }
                }
            }
            TickBehavior();
        }

        protected virtual void TickBehavior() { }

        protected virtual void OnEnterRange(IEntity entity, ServerOverlayEffectsComponent overlayEffects){ }

        protected virtual void OnExitRange(IEntity entity, ServerOverlayEffectsComponent overlayEffects) { }
    }
}
