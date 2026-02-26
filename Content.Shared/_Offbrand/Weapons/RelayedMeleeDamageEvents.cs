using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Offbrand.Weapons;

[ByRefEvent]
public record struct RelayedGetMeleeDamageEvent(GetMeleeDamageEvent Args);

[ByRefEvent]
public record struct RelayedGetMeleeAttackRateEvent(GetMeleeAttackRateEvent Args);
