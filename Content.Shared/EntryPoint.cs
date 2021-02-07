using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Shared.Chemistry;
using Content.Shared.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared
{
    public class EntryPoint : GameShared
    {
        // If you want to change your codebase's language, do it here.
        private const string Culture = "en-US";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        public override void PreInit()
        {
            IoCManager.InjectDependencies(this);

            var textMacroFactory = IoCManager.Resolve<ITextMacroFactory>();
            textMacroFactory.DoAutoRegistrations();

            // Default to en-US.
            Loc.LoadCulture(new CultureInfo(Culture));
        }

        public override void Init()
        {
        }

        public override void PostInit()
        {
            base.PostInit();

            _initTileDefinitions();
            CheckReactions();
        }

        private void CheckReactions()
        {
            foreach (var reaction in _prototypeManager.EnumeratePrototypes<ReactionPrototype>())
            {
                foreach (var reactant in reaction.Reactants.Keys)
                {
                    if (!_prototypeManager.HasIndex<ReagentPrototype>(reactant))
                    {
                        Logger.ErrorS(
                            "chem", "Reaction {reaction} has unknown reactant {reagent}.",
                            reaction.ID, reactant);
                    }
                }

                foreach (var product in reaction.Products.Keys)
                {
                    if (!_prototypeManager.HasIndex<ReagentPrototype>(product))
                    {
                        Logger.ErrorS(
                            "chem", "Reaction {reaction} has unknown product {product}.",
                            reaction.ID, product);
                    }
                }
            }
        }

        private void _initTileDefinitions()
        {
            // Register space first because I'm a hard coding hack.
            var spaceDef = _prototypeManager.Index<ContentTileDefinition>("space");

            _tileDefinitionManager.Register(spaceDef);

            var prototypeList = new List<ContentTileDefinition>();
            foreach (var tileDef in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
            {
                if (tileDef.Name == "space")
                {
                    continue;
                }

                prototypeList.Add(tileDef);
            }

            // Sort ordinal to ensure it's consistent client and server.
            // So that tile IDs match up.
            prototypeList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (var tileDef in prototypeList)
            {
                _tileDefinitionManager.Register(tileDef);
            }

            _tileDefinitionManager.Initialize();
        }
    }
}
