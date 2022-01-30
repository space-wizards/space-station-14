using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Sprite.Components
{
    [RegisterComponent, ComponentProtoName("RandomSpriteState")]
    public class RandomSpriteStateComponent : Component
    {
        [DataField("spriteStates")] public List<string>? SpriteStates;

        [DataField("spriteLayer")] public int SpriteLayer;
    }
}
