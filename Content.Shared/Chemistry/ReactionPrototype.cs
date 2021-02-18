#nullable enable
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public class ReactionPrototype : IPrototype, IIndexedPrototype
    {
        [Dependency] private readonly IModuleManager _moduleManager = default!;

        [DataField("id")] private string _id = default!;
        [DataField("name")] private string _name = default!;
        [DataField("reactants")] private Dictionary<string, ReactantPrototype> _reactants = default!;
        [DataField("products")] private Dictionary<string, ReagentUnit> _products = default!;
        [DataField("effects")] private List<IReactionEffect> _effects = default!;

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

        // TODO SERV3: Empty on the client, (de)serialize on the server with module manager is server module
        [DataField("sound")] public string? Sound { get; private set; } = "/Audio/Effects/Chemistry/bubbles.ogg";
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
