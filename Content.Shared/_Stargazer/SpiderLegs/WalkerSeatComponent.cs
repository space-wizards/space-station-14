// Content.Shared/_Stargazer/SpiderLegs/WalkerSeatComponent.cs
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Stargazer.SpiderLegs;

/// <summary>
/// Своя копия StrapVehicle для шагающего кресла: пристёгнутый становится водителем,
/// но с "мехскими" свойствами:
/// - сесть можно только после do-after (MountDelay);
/// - другим, чтобы стащить водителя, нужен долгий do-after (DismountDelay); сам водитель слезает сразу;
/// - пока водитель сидит, он получает меньше урона (RiderArmor).
/// Водитель сидит снаружи, поэтому руки свободны: может стрелять и пользоваться предметами.
/// </summary>
[RegisterComponent]
public sealed partial class WalkerSeatComponent : Component
{
    /// <summary>Сколько секунд забираться в кресло. 0 — мгновенно.</summary>
    [DataField]
    public float MountDelay = 2f;

    /// <summary>Сколько секунд другим нужно, чтобы стащить водителя. 0 — мгновенно.</summary>
    [DataField]
    public float DismountDelay = 8f;

    /// <summary>Модификаторы урона для водителя, пока он сидит. null — без брони.</summary>
    [DataField]
    public DamageModifierSet? RiderArmor;

    /// <summary>Кого сейчас сажаем по завершённому do-after (чтобы не запустить его повторно).</summary>
    [ViewVariables]
    public EntityUid? PendingMount;

    /// <summary>Кого сейчас стаскиваем по завершённому do-after.</summary>
    [ViewVariables]
    public EntityUid? PendingDismount;
}

/// <summary>
/// Ставится на водителя, пока он сидит в шагающем кресле. Нужен для брони водителя.
/// </summary>
[RegisterComponent]
public sealed partial class WalkerRiderComponent : Component
{
    [ViewVariables]
    public EntityUid Seat;
}

[Serializable, NetSerializable]
public sealed partial class WalkerSeatMountDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class WalkerSeatDismountDoAfterEvent : SimpleDoAfterEvent;
