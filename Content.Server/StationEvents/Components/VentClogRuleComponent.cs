using Content.Server.StationEvents.Events;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentClogRule))]
public sealed partial class VentClogRuleComponent : Component
{
    /// <summary>
    /// Chance of each individual vent being affected by this rule (having something flow out of it)
    /// </summary>
    [DataField]
    public float VentClogChance = 0.33f;

    /// <summary>
    /// Chance of an affected vent rolling from the table of *ALL* reagents instead of just the <see cref="SafeishVentChemicals"/>
    /// </summary>
    [DataField]
    public float AnyReagentChance = 0.05f;
    
    /// <summary>
    /// Somewhat safe chemicals to put in foam that probably won't instantly kill you.
    /// There is a small chance of using any reagent, ignoring this (see <see cref="AnyReagentChance"/>).
    /// </summary>
    [DataField]
    public IReadOnlyList<ProtoId<ReagentPrototype>> SafeishVentChemicals = new ProtoId<ReagentPrototype>[]
    {
        "Water", "Blood", "Slime", "SpaceDrugs", "SpaceCleaner", "Nutriment", "Sugar", "SpaceLube", "Ephedrine", "Ale", "Beer", "SpaceGlue"
    };

    /// <summary>
    /// Sound played when foam is being created.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/extinguish.ogg");

    /// <summary>
    /// The standard reagent quantity to put in the foam, modified by event severity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ReagentQuantity = 100;

    /// <summary>
    /// The standard spreading of the foam, not modified by event severity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Spread = 16;

    /// <summary>
    /// How long the foam lasts for
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Time = 20f;

    /// <summary>
    /// Reagents that gets the weak numbers used instead of regular ones.
    /// </summary>
    [DataField]
    public IReadOnlyList<ProtoId<ReagentPrototype>> WeakReagents = new ProtoId<ReagentPrototype>[]
    {
        "SpaceLube", "SpaceGlue"
    };

    /// <summary>
    /// Quantity of weak reagents to put in the foam.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int WeakReagentQuantity = 50;

    /// <summary>
    /// Spread of the foam for weak reagents.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int WeakSpread = 3;
}
