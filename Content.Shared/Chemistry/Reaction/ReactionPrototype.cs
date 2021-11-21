using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Sound;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Reaction
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public class ReactionPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Reactants required for the reaction to occur.
        /// </summary>
        [DataField("reactants")] public Dictionary<string, ReactantPrototype> Reactants = new();

        /// <summary>
        /// Reagents created when the reaction occurs.
        /// </summary>
        [DataField("products")] public Dictionary<string, FixedPoint2> Products = new();

        /// <summary>
        /// Effects to be triggered when the reaction occurs.
        /// </summary>
        [DataField("effects", serverOnly: true)] public List<ReagentEffect> Effects = new();

        // TODO SERV3: Empty on the client, (de)serialize on the server with module manager is server module
        [DataField("sound", serverOnly: true)] public SoundSpecifier Sound { get; private set; } = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
    }

    /// <summary>
    /// Prototype for chemical reaction reactants.
    /// </summary>
    [DataDefinition]
    public class ReactantPrototype
    {
        [DataField("amount")]
        private FixedPoint2 _amount = FixedPoint2.New(1);
        [DataField("catalyst")]
        private bool _catalyst;

        /// <summary>
        /// Minimum amount of the reactant needed for the reaction to occur.
        /// </summary>
        public FixedPoint2 Amount => _amount;
        /// <summary>
        /// Whether or not the reactant is a catalyst. Catalysts aren't removed when a reaction occurs.
        /// </summary>
        public bool Catalyst => _catalyst;
    }
}
