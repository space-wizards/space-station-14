using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed class DamageOnToolInteractComponent : Component
    {
        [DataField("tools")]
        public PrototypeFlags<ToolQualityPrototype> Tools { get; } = new ();

        // TODO: Remove this snowflake stuff, make damage per-tool quality perhaps?
        [DataField("weldingDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier? WeldingDamage { get; }

        [DataField("defaultDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier? DefaultDamage { get; }
    }
}
