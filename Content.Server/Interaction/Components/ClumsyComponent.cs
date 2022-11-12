using Content.Shared.Damage;

namespace Content.Server.Interaction.Components
{
    /// <summary>
    /// A simple clumsy tag-component.
    /// </summary>
    [RegisterComponent]
    public sealed class ClumsyComponent : Component
    {
        [DataField("clumsyDamage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier ClumsyDamage = default!;
    }
}
