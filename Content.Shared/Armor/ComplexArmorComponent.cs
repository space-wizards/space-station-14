using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Armor;

/// <summary>
///     Used for clothing that reduces damage when worn with specific clothing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedArmorSystem))]
public sealed partial class ComplexArmorComponent : Component
{
    /// <summary>
    ///     The damage reduction.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageModifierSet Modifiers = default!;

    /// <summary>
    ///     Slots where clothing with tags should.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.HEAD;

    /// <summary>
    ///     Tag that worn clothing should have for resists.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<TagPrototype>[] Tags = [];
}
