using Robust.Shared.Serialization;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Client floating combat feedback: degree label (Hit/Miss/…) plus optional damage amount.
/// </summary>
[Serializable, NetSerializable]
public sealed class CFCombatNumberEvent : EntityEventArgs
{
    public NetEntity Target;
    public CFStrikeDegree Degree;
    public int Amount;
    public string DamageType = string.Empty;

    public CFCombatNumberEvent(NetEntity target, CFStrikeDegree degree, int amount, string damageType)
    {
        Target = target;
        Degree = degree;
        Amount = amount;
        DamageType = damageType;
    }
}
