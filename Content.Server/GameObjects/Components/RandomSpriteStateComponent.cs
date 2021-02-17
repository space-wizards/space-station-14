using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RandomSpriteStateComponent : Component
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override string Name => "RandomSpriteState";

        [DataField("spriteStates")]
        private List<string> _spriteStates;

        [DataField("spriteLayer")]
        private int _spriteLayer;

        public override void Initialize()
        {
            base.Initialize();
            if (_spriteStates == null) return;
            if (!Owner.TryGetComponent(out SpriteComponent spriteComponent)) return;
            spriteComponent.LayerSetState(_spriteLayer, _random.Pick(_spriteStates));
        }
    }
}
