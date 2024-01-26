using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

public abstract class SharedMedipenRefillerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Compare the contents of the buffer with what is in the recipe, performing a small conversion for this.
    /// If it's EXACTILY the same reagents and there is a medipen inserted, it will be possible to make a new one.
    /// </summary>
    public bool CanRefill(string id, List<ReagentQuantity> content, bool isInserted)
    {
        if (!isInserted)
            return false;

        var recipes = _prototypeManager.EnumeratePrototypes<MedipenRecipePrototype>();

        var requiredReagents = new Dictionary<ReagentId, FixedPoint2>();

        foreach (var recipe in recipes!)
        {
            if (recipe.ID.Equals(id))
            {
                foreach (var reagent in recipe.RequiredReagents)
                {
                    requiredReagents.Add(new ReagentId(reagent.Key, null), reagent.Value);
                }
            }
        }

        if (content.Count.Equals(requiredReagents.Count))
        {
            foreach (var reagent in content)
            {
                if (!requiredReagents.ContainsKey(reagent.Reagent) || !requiredReagents[reagent.Reagent].Equals(reagent.Quantity))
                    return false;
            }
            return true;
        }

        return false;
    }

    [Serializable, NetSerializable]
    public enum MedipenRefillerUiKey
    {
        Key
    }
}
