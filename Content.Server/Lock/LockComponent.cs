using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    /// <summary>
    /// Allows locking/unlocking, with access determined by AccessReader
    /// </summary>
    [RegisterComponent]
    public sealed class LockComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("locked")] public bool Locked { get; set; } = true;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("lockOnClick")] public bool LockOnClick { get; set; } = false;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("unlockingSound")] public SoundSpecifier UnlockSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");
        [ViewVariables(VVAccess.ReadWrite)] [DataField("lockingSound")] public SoundSpecifier LockSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");
    }
}
