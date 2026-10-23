namespace Content.Server.PdaMessenger;

/// <summary>
///     Gives an ID card a messenger user ID, so its messages follow the card to any PDA.
/// </summary>
[RegisterComponent, Access(typeof(PdaMessengerCartridgeSystem))]
public sealed partial class PdaMessengerIdComponent : Component
{
    /// <summary>
    ///     The card's messenger user ID. Assigned when the card spawns, unless set in YAML.
    /// </summary>
    [DataField]
    public string? UserId;
}
