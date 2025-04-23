using Robust.Shared.GameStates;

namespace Content.Server._Impstation.EquipmentHeater;

/// <summary>
/// Used to make clothing that increases a player's temperature over time when equipped. 
/// </summary>
[RegisterComponent]
public sealed partial class EquipmentHeaterComponent : Component
{
    /// <summary>
    /// The time between temperature increases. 
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The amount to increase the equipee's temperature each update. 
    /// At its default setting, this will hover just below the damage threshold if the room they are in is at ambient temperature. 
    /// </summary>
    [DataField]
    public float TempIncrease = 2f;

    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Stores the UID of the person who currently has this equipped. Reverts to null when unequipped.
    /// </summary>
    public EntityUid? CurrentlyEquipped = null;
}
