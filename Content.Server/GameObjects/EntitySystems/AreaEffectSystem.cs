using System.Collections.Generic;
using Content.Server.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AreaEffectSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly HashSet<AreaEffectInception> _inceptions = new();

        private readonly HashSet<AreaEffectInception> _toRemove = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AreaEffectInceptionCreatedMessage>(HandleAreaEffectInceptionCreatedMessage);
        }

        private void HandleAreaEffectInceptionCreatedMessage(AreaEffectInceptionCreatedMessage message)
        {
            _inceptions.Add(message.Inception);
        }

        public override void Update(float frameTime)
        {
            foreach (var inception in _inceptions)
            {
                if (inception.InceptionUpdate(frameTime, _mapManager, _prototypeManager))
                    _toRemove.Add(inception);
            }
            _inceptions.ExceptWith(_toRemove);
            _toRemove.Clear();
        }
    }
}
