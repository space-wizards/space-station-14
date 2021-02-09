using System.Collections.Generic;
using Content.Server.Interfaces.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Chemistry
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public class ReactionPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("id")]
        private string _id;
        [YamlField("name")]
        private string _name;
        [YamlField("reactants")]
        private Dictionary<string, ReactantPrototype> _reactants = new();
        [YamlField("products")]
        private Dictionary<string, ReagentUnit> _products = new();
        [YamlField("effects")]
        private List<IReactionEffect> _effects = new();

        public string ID => _id;
        public string Name => _name;
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
    }

    /// <summary>
    /// Prototype for chemical reaction reactants.
    /// </summary>
    [YamlDefinition]
    public class ReactantPrototype
    {
        [YamlField("amount")]
        private ReagentUnit _amount = ReagentUnit.New(1);
        [YamlField("catalyst")]
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
