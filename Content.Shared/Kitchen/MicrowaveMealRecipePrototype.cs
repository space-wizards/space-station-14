using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>

    [Prototype("microwaveMealRecipe")]

    public class FoodRecipe : IPrototype, IIndexedPrototype
    {

        public string ID {get; private set;}
        public string Name {get; private set;}

        public string Description {get; private set;}

        public Dictionary<string,int>  Ingredients {get; private set;}

        private const char Seperator = ',';

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").ToString();
            Name = Loc.GetString(mapping.GetNode("name").ToString());
            Description = Loc.GetString(mapping.GetNode("description").ToString());
            if(mapping.TryGetNode("ingredients", out YamlSequenceNode tempDict))
            {
                Ingredients = new Dictionary<string, int>();
                foreach (var node in tempDict.Children)
                {
                    var pair = node.ToString();
                    if (pair == null) continue;

                    var split = pair.Split(Seperator);
                    var ingnName = split[0];
                    if (int.TryParse(split[1], out var amt)) Ingredients.Add(ingnName, amt);

                }
            }

        }
    }
}
