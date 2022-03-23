using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SharedAirlockPainter
{
    [NetworkedComponent]
    public abstract class SharedAirlockPainterComponent : Component
    {
        [DataField("spriteList")]
        public List<string> SpriteList = new();

        [DataField("whitelist")]
        public EntityWhitelist Whitelist = new();
    }
}
