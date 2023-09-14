using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]
public sealed partial class PiratesRuleComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Pirates = new();
    [ViewVariables]
    public EntityUid PirateShip = EntityUid.Invalid;
    [ViewVariables]
    public HashSet<EntityUid> InitialItems = new();
    [ViewVariables]
    public double InitialShipValue;

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("pirateAlertSound")]
    public SoundSpecifier PirateAlertSound = new SoundPathSpecifier(
        "/Audio/Ambience/Antag/pirate_start.ogg",
        AudioParams.Default.WithVolume(4));
}
