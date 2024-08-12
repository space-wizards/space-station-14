using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Roles;

/// <summary>
///     For selectable ghostrole prototypes in ghostrole spawners.
/// </summary>
[Prototype]
public sealed partial class GhostRolePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the ghostrole.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; set; } = default!;

    /// <summary>
    ///     The description of the ghostrole.
    /// </summary>
    [DataField(required: true)]
    public string Description { get; set; } = default!;

    /// <summary>
    ///     The entity prototype of the ghostrole
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EntityPrototype;

    /// <summary>
    /// The entity prototype's sprite to use to represent the ghost role
    /// Use this if you don't want to use the entity itself
    /// </summary>
    [DataField]
    public EntProtoId? IconPrototype = null;

    /// <summary>
    ///     Rules of the ghostrole
    /// </summary>
    [DataField(required: true)]
    public string Rules = default!;
}
