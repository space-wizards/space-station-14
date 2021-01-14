using System.Collections.Generic;
using Content.Server.Interfaces.Chemistry;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public class ReactionPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private Dictionary<string, ReactantPrototype> _reactants;
        private Dictionary<string, ReagentUnit> _products;
        private List<IReactionEffect> _effects;

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

        public string Sound { get; private set; }

        [Dependency] private readonly IModuleManager _moduleManager = default!;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _reactants, "reactants", new Dictionary<string, ReactantPrototype>());
            serializer.DataField(ref _products, "products", new Dictionary<string, ReagentUnit>());
            serializer.DataField(this, x => x.Sound, "sound", "/Audio/Effects/Chemistry/bubbles.ogg");

            if (_moduleManager.IsServerModule)
            {
                //TODO: Don't have a check for if this is the server
                //Some implementations of IReactionEffect can't currently be moved to shared, so this is here to prevent the client from breaking when reading server-only IReactionEffects.
                serializer.DataField(ref _effects, "effects", new List<IReactionEffect>());
            }
        }
    }

    /// <summary>
    /// Prototype for chemical reaction reactants.
    /// </summary>
    public class ReactantPrototype : IExposeData
    {
        private ReagentUnit _amount;
        private bool _catalyst;

        /// <summary>
        /// Minimum amount of the reactant needed for the reaction to occur.
        /// </summary>
        public ReagentUnit Amount => _amount;
        /// <summary>
        /// Whether or not the reactant is a catalyst. Catalysts aren't removed when a reaction occurs.
        /// </summary>
        public bool Catalyst => _catalyst;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _amount, "amount", ReagentUnit.New(1));
            serializer.DataField(ref _catalyst, "catalyst", false);
        }
    }
}
