using Content.Shared.Sound;
using Content.Shared.Tools;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Tools.Components
{
    [RegisterComponent, Friend(typeof(ToolSystem))]
    public sealed class ToolComponent : Component
    {
        [DataField("qualities")]
        public PrototypeFlags<ToolQualityPrototype> Qualities { get; set; } = new();

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        public float SpeedModifier { get; set; } = 1;

        [DataField("useSound")]
        public SoundSpecifier? UseSound { get; set; }
    }
}
