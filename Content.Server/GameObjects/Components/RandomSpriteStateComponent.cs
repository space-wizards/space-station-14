using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RandomSpriteStateComponent : Component
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override string Name => "RandomSpriteState";

        private List<string> _spriteStates;

        private int _spriteLayer;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _spriteStates, "spriteStates", null);
            serializer.DataField(ref _spriteLayer, "spriteLayer", 0);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_spriteStates == null) return;
            if (!Owner.TryGetComponent(out SpriteComponent spriteComponent)) return;
            spriteComponent.LayerSetState(_spriteLayer, _random.Pick(_spriteStates));
        }
    }
}
