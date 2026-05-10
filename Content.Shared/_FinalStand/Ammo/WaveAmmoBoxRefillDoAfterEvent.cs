using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._FinalStand.Ammo;

[Serializable, NetSerializable]
public sealed partial class WaveAmmoBoxRefillDoAfterEvent : SimpleDoAfterEvent;
