using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Flash.Components
{
    /// <summary>
    /// Upon colliding with an object this will flash in an area around it
    /// </summary>
    [RegisterComponent]
    internal sealed class FlashAreaOnCollide : Component
    {
        public override string Name => "FlashAreaOnCollide";

        [DataField("range")] internal float Range = 1.0f;
        [DataField("duration")] internal float Duration = 8.0f;

        internal bool Flashed;
    }
}
