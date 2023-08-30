using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.Utility;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed partial class DamageOnToolInteractComponent : Component
    {
        [DataField("tools")]
        public PrototypeFlags<ToolQualityPrototype> Tools { get; private set; } = new ();

        // TODO: Remove this snowflake stuff, make damage per-tool quality perhaps?
        [DataField("weldingDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier? WeldingDamage { get; private set; }

        [DataField("defaultDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier? DefaultDamage { get; private set; }
    }
}
