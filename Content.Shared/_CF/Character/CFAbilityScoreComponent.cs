using Content.Shared.CF.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared.CF.Character;

/// <summary>
/// Pathfinder 2e attribute modifiers (not D&amp;D ability scores). Default is +0.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFAbilityScoreComponent : Component
{
    [DataField("abilityModifiers")]
    [AutoNetworkedField]
    public Dictionary<CFAbility, int> AbilityModifiers = new()
    {
        { CFAbility.Strength, 0 },
        { CFAbility.Dexterity, 0 },
        { CFAbility.Constitution, 0 },
        { CFAbility.Intelligence, 0 },
        { CFAbility.Wisdom, 0 },
        { CFAbility.Charisma, 0 },
    };

    public int GetModifier(CFAbility ability)
    {
        return AbilityModifiers.TryGetValue(ability, out var modifier) ? modifier : 0;
    }

    /// <summary>Legacy name — returns the PF2e modifier.</summary>
    public int GetAbilityScore(CFAbility ability) => GetModifier(ability);
}