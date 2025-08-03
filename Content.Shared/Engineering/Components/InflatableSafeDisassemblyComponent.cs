using Content.Shared.Engineering.Systems;
using Content.Shared.Weapons.Melee.Balloon;

namespace Content.Shared.Engineering.Components;

/// <summary>
/// Implements logic to allow inflatable objects to be safely deflated by <see cref="BalloonPopperComponent"/> items.
/// </summary>
/// <remarks>
/// The owning entity must have <see cref="DisassembleOnAltVerbComponent"/> to implement the logic.
/// </remarks>
/// <seealso cref="InflatableSafeDisassemblySystem"/>
[RegisterComponent]
public sealed partial class InflatableSafeDisassemblyComponent : Component;
