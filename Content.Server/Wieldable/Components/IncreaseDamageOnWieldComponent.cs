using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wieldable.Components
{
    [RegisterComponent, Friend(typeof(WieldableSystem))]
    public class IncreaseDamageOnWieldComponent : Component
    {
        public override string Name { get; } = "IncreaseDamageOnWield";

        // TODO Change to use resistanceset/damageset/whatever so this can be of arbitrary type

        [DataField("damageMultiplier")]
        public int DamageMultiplier = 1;

        [DataField("flatDamage")]
        public int FlatDamage = 0;
    }
}
