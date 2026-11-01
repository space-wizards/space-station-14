using Robust.Shared.GameStates;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder combat stats (AC / HP) for player characters and mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFCombatantComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Level = 1;

    [DataField, AutoNetworkedField]
    public int ArmorClass = 10;

    [DataField, AutoNetworkedField]
    public int MaxHp = 10;

    [DataField, AutoNetworkedField]
    public int CurrentHp = 10;

    [DataField, AutoNetworkedField]
    public string? ClassId;

    [DataField, AutoNetworkedField]
    public string? BackgroundId;

    /// <summary>
    /// Extra trained skills from background picks and free class skill picks
    /// (class fixed skills stay on the class proficiency list).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> TrainedSkills = new();

    /// <summary>Specific Lore names from the chosen ancestry feat, shown on the Lore row.</summary>
    [DataField, AutoNetworkedField]
    public List<string> LoreNames = new();

    [DataField, AutoNetworkedField]
    public int TemporaryHp;

    [DataField, AutoNetworkedField]
    public int Dying;

    [DataField, AutoNetworkedField]
    public int Wounded;
}
