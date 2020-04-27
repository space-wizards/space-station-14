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

    public class MicrowaveMealRecipePrototype : IPrototype, IIndexedPrototype
    {

        public string ID {get; private set;}
        public string Name {get; private set;}

        public string OutPutPrototype { get; private set; }
        public Dictionary<string,int> Ingredients {get; private set;}
        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").ToString();
            Name = Loc.GetString(mapping.GetNode("name").ToString());
            OutPutPrototype = mapping.GetNode("output").ToString();
            if(mapping.TryGetNode("ingredients", out YamlMappingNode ingDict))
            {
                Ingredients = new Dictionary<string, int>();
                foreach (var kvp in ingDict.Children)
                {
                    Ingredients.Add(kvp.Key.ToString(), kvp.Value.AsInt());
                }
            }

        }
    }
}
