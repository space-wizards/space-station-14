using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Traits;

/// <summary>
/// Traits on a spawned entity: a weapon, armor, creature, or other object.
/// Prototypes that are not entities, such as spells, store the same ids on their own trait list.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFTraitComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CFTraitPrototype>> Traits = new();

    public bool HasTrait(ProtoId<CFTraitPrototype> trait)
    {
        return Traits.Contains(trait);
    }

    public bool HasAny(IReadOnlyList<ProtoId<CFTraitPrototype>> traits)
    {
        foreach (var trait in traits)
        {
            if (Traits.Contains(trait))
                return true;
        }

        return false;
    }
}
