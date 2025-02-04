using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.EnergySword;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class EnergySwordComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color ActivatedColor = Color.DodgerBlue;

    /// <summary>
    ///     A color option list for the random color picker.
    /// </summary>
    [DataField("colorOptions")]
    public List<Color> ColorOptions = new()
    {
        Color.Tomato,
        Color.DodgerBlue,
        Color.Aqua,
        Color.MediumSpringGreen,
        Color.MediumOrchid
    };

    [DataField, AutoNetworkedField]
    public bool Hacked = false;
    /// <summary>
    ///     RGB cycle rate for hacked e-swords.
    /// </summary>
    [DataField("cycleRate")]
    public float CycleRate = 1f;
}
