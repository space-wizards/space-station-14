using Robust.Shared.GameStates;

namespace Content.Shared.CF.Spells;

/// <summary>
/// Food that prestidigitation has flavoured. Eating it adds the magical flavor on top of the food's own.
/// When a raw item cooks, construction replaces the entity, and this component is copied onto the result.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFSpellSeasoningComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Magical = true;
}
