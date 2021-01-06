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
    public class SmokeSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly HashSet<SmokeInception> _smokeInceptions = new();

        private readonly HashSet<SmokeInception> _toRemove = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SmokeInceptionCreatedMessage>(HandleSmokeInceptionCreatedMessage);
        }

        private void HandleSmokeInceptionCreatedMessage(SmokeInceptionCreatedMessage message)
        {
            _smokeInceptions.Add(message.Inception);
        }

        public override void Update(float frameTime)
        {
            foreach (var smokeInception in _smokeInceptions)
            {
                if (smokeInception.InceptionUpdate(frameTime, _mapManager, _prototypeManager))
                    _toRemove.Add(smokeInception);
            }
            _smokeInceptions.ExceptWith(_toRemove);
            _toRemove.Clear();
        }
    }
}
