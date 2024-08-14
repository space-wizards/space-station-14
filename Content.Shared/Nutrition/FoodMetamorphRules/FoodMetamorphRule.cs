using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.FoodMetamorphRules;
/// <summary>
/// abstract rules that are used to verify the correct foodSequence for recipe
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class FoodMetamorphRule
{
    public abstract bool Check();
}
