using Content.Shared.Whitelist;

namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    public sealed class AirlockPainterComponent : Component
    {
        [DataField("spriteList")]
        public List<string> SpriteList = new();

        [DataField("whitelist")]
        public EntityWhitelist Whitelist = new();

        [DataField("index")]
        public int Index = 0;
    }
}
