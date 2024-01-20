namespace Content.Server.Weapons.Melee.EnergySword;

[RegisterComponent]
internal sealed partial class EnergySwordComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("activatedColor"), AutoNetworkedField]
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

    public bool Hacked = false;
    /// <summary>
    ///     RGB cycle rate for hacked e-swords.
    /// </summary>
    [DataField("cycleRate")]
    public float CycleRate = 1f;
}
