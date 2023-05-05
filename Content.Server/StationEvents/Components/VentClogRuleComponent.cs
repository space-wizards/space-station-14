using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentClogRule))]
public sealed class VentClogRuleComponent : Component
{
    /// <summary>
    /// Somewhat safe chemicals to put in foam that probably won't instantly kill you.
    /// There is a small chance of using any reagent, ignoring this.
    /// </summary>
    [DataField("safeishVentChemicals")]
    public readonly IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Blood", "Slime", "SpaceDrugs", "SpaceCleaner", "Nutriment", "Sugar", "SpaceLube", "Ephedrine", "Ale", "Beer"
    };

    /// <summary>
    /// Sound played when foam is being created.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/extinguish.ogg");

    /// <summary>
    /// The standard reagent quantity to put in the foam, modfied by event severity.
    /// </summary>
    [DataField("reagentQuantity"), ViewVariables(VVAccess.ReadWrite)]
    public int ReagentQuantity = 200;

    /// <summary>
    /// The standard spreading of the foam, modfied by event severity.
    /// </summary>
    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public int Spread = 20;

    /// <summary>
    /// How long the foam lasts for
    /// </summary>
    [DataField("time"), ViewVariables(VVAccess.ReadWrite)]
    public float Time = 20f;

    /// <summary>
    /// A reagent that gets the "evil" numbers used instead of regular ones, if any.
    /// </summary>
    [DataField("evilReagent"), ViewVariables(VVAccess.ReadWrite)]
    public string? EvilReagent = "SpaceLube";

    /// <summary>
    /// Quantity of the evil reagent to put in the foam.
    /// </summary>
    [DataField("evilReagentQuantity"), ViewVariables(VVAccess.ReadWrite)]
    public int EvilReagentQuantity = 60;

    /// <summary>
    /// Spread of the foam for the evil reagent.
    /// </summary>
    [DataField("evilSpread"), ViewVariables(VVAccess.ReadWrite)]
    public int EvilSpread = 2;
}
