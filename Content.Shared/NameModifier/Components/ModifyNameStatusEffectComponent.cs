using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.NameModifier.Components;

/// <summary>
/// Adds a name modifier to an entity afflicted by the status effect, and removes it when recovered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(NameModifierSystem))]
public sealed partial class ModifyNameStatusEffectComponent : Component
{
    /// <summary>
    /// The localization ID of the name modifier.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>baseName</c> - The original name of the entity.</item>
    /// </list>
    /// </value>
    [DataField, AutoNetworkedField]
    public LocId LocId = string.Empty;

    /// <summary>
    /// Priority of the modifier. See <see cref="EntitySystems.RefreshNameModifiersEvent.AddModifier"/> for more information.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority;
}
