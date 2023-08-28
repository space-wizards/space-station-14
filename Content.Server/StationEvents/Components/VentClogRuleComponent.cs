using Content.Server.StationEvents.Events;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentClogRule))]
public sealed partial class VentClogRuleComponent : Component
{
    /// <summary>
    /// Somewhat safe chemicals to put in foam that probably won't instantly kill you.
    /// There is a small chance of using any reagent, ignoring this.
    /// </summary>
    [DataField("safeishVentChemicals", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Blood", "Slime", "SpaceDrugs", "SpaceCleaner", "Nutriment", "Sugar", "SpaceLube", "Ephedrine", "Ale", "Beer", "SpaceGlue"
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
    /// The standard spreading of the foam, not modfied by event severity.
    /// </summary>
    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public int Spread = 20;

    /// <summary>
    /// How long the foam lasts for
    /// </summary>
    [DataField("time"), ViewVariables(VVAccess.ReadWrite)]
    public float Time = 20f;

    /// <summary>
    /// Reagents that gets the weak numbers used instead of regular ones.
    /// </summary>
    [DataField("weakReagents", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public IReadOnlyList<string> WeakReagents = new[]
    {
        "SpaceLube", "SpaceGlue"
    };

    /// <summary>
    /// Quantity of weak reagents to put in the foam.
    /// </summary>
    [DataField("weakReagentQuantity"), ViewVariables(VVAccess.ReadWrite)]
    public int WeakReagentQuantity = 60;

    /// <summary>
    /// Spread of the foam for weak reagents.
    /// </summary>
    [DataField("weakSpread"), ViewVariables(VVAccess.ReadWrite)]
    public int WeakSpread = 2;
}
