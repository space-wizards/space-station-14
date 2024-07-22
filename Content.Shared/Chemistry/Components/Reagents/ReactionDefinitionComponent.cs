using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReactionDefinitionComponent : Component
{
    [DataField(required: true)]
    public string Id = string.Empty;

    /// <summary>
    /// Determines the order in which reactions occur. This should used to ensure that (in general) descriptive /
    /// pop-up generating and explosive reactions occur before things like foam/area effects.
    /// </summary>
    [DataField]
    public int Priority;

    [DataField]
    public Dictionary<string, ReactantData> Reactants = new();

    /// <summary>
    /// Reagents created when the reaction occurs.
    /// </summary>
    [DataField]
    public Dictionary<string, FixedPoint2> Products = new();

    /// <summary>
    /// Effects to be triggered when the reaction occurs.
    /// </summary>
    [DataField(serverOnly: true)] public List<EntityEffect> Effects = new();

    /// <summary>
    /// If true, this reaction will only consume only integer multiples of the reactant amounts. If there are not
    /// enough reactants, the reaction does not occur. Useful for spawn-entity reactions (e.g. creating cheese).
    /// </summary>
    [DataField] public bool Quantized;

    /// <summary>
    ///     If true, this reaction will attempt to conserve thermal energy.
    /// </summary>
    [DataField]
    public bool ConserveEnergy = true;

    /// <summary>
    /// How dangerous is this effect? Stuff like bicaridine should be low, while things like methamphetamine
    /// or potas/water should be high.
    /// </summary>
    [DataField(serverOnly: true)] public LogImpact Impact = LogImpact.Low;

    // TODO SERV3: Empty on the client, (de)serialize on the server with module manager is server module
    [DataField(serverOnly: true)] public SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");

    public ReactionType ReactionType
    {
        get
        {
            if (Products.Count == 0)
            {
                return ReactionType.Absorption;
            }
            if (Reactants.Count == 1)
            {
                return ReactionType.Decomposition;
            }
            return ReactionType.Synthesis;
        }
    }

    public bool Source => ReactionType == ReactionType.Decomposition;

    public bool Absorption => ReactionType == ReactionType.Absorption;

    [DataField]
    public bool Catalyzed;

    [DataField]
    public Dictionary<EntityUid, ReactantData> ReactantEntities = new();

    [DataField]
    public Dictionary<EntityUid, FixedPoint2> ProductEntities = new();

    [DataField]
    public string LegacyId = string.Empty;
}


/// <summary>
/// Prototype for chemical reaction reactants.
/// </summary>
[DataRecord]
public record struct ReactantData(FixedPoint4 Amount, bool Catalyst = false);


public enum ReactionType : byte
{
    Unknown,
    Decomposition,
    Synthesis,
    Absorption
}
