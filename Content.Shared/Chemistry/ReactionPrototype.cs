#nullable enable
using System.Collections.Generic;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public class ReactionPrototype : IPrototype
    {
        [DataField("reactants")] private Dictionary<string, ReactantPrototype> _reactants = new();
        [DataField("products")] private Dictionary<string, ReagentUnit> _products = new();
        [DataField("effects", serverOnly: true)] private List<IReactionEffect> _effects = new();

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Reactants required for the reaction to occur.
        /// </summary>
        public IReadOnlyDictionary<string, ReactantPrototype> Reactants => _reactants;
        /// <summary>
        /// Reagents created when the reaction occurs.
        /// </summary>
        public IReadOnlyDictionary<string, ReagentUnit> Products => _products;
        /// <summary>
        /// Effects to be triggered when the reaction occurs.
        /// </summary>
        public IReadOnlyList<IReactionEffect> Effects => _effects;

        // TODO SERV3: Empty on the client, (de)serialize on the server with module manager is server module
        [DataField("sound", serverOnly: true)] public string? Sound { get; private set; } = "/Audio/Effects/Chemistry/bubbles.ogg";
    }

    /// <summary>
    /// Prototype for chemical reaction reactants.
    /// </summary>
    [DataDefinition]
    public class ReactantPrototype
    {
        [DataField("amount")]
        private ReagentUnit _amount = ReagentUnit.New(1);
        [DataField("catalyst")]
        private bool _catalyst;

        /// <summary>
        /// Minimum amount of the reactant needed for the reaction to occur.
        /// </summary>
        public ReagentUnit Amount => _amount;
        /// <summary>
        /// Whether or not the reactant is a catalyst. Catalysts aren't removed when a reaction occurs.
        /// </summary>
        public bool Catalyst => _catalyst;
    }
}
