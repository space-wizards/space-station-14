using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CPR;

/// <summary>
/// Stores info about the mob's CPR mechanics
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CPRComponent : Component
{
    // this has to be initialized in the component because otherwise theres a really weird edge case bug where cpr causes a cauterize sound when bleeding
    [DataField("heal")]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Heal = new()
    {
        DamageDict =
        {
            ["Asphyxiation"] = -2,
            ["Bloodloss"] = -0.5,
        }
    };

    [DataField("bonusHeal")]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier BonusHeal = new()
    {
        DamageDict =
        {
            ["Asphyxiation"] = -5,
            ["Bloodloss"] = -1.5,
        }
    };

    [DataField("damage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            ["Blunt"] = 0.2
        }
    };


    [DataField("sound")]
    public SoundSpecifier? Sound = null;
}
