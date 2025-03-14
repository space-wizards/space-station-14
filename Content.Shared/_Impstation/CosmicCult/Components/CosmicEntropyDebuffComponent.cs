using Content.Shared.Damage;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Makes the target take damage over time.
/// Meant to be used in conjunction with statusEffectSystem.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[AutoGenerateComponentState]
public sealed partial class CosmicEntropyDebuffComponent : Component
{
    [AutoPausedField] public TimeSpan CheckTimer = default!;

    [DataField, AutoNetworkedField]
    public TimeSpan CheckWait = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The chance to recieve a message popup while under the effects of Entropic Degen.
    /// </summary>
    [DataField]
    public float PopupChance = 0.05f;
    
    /// <summary>
    /// The debuff applied while the component is present.
    /// </summary>
    [DataField]
    public DamageSpecifier Degen = new()
    {
        DamageDict = new()
        {
            { "Cold", 0.25},
            { "Asphyxiation", 1.25},
        }
    };
}
