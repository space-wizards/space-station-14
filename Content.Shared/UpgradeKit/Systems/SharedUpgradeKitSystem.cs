using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.UpgradeKit.Systems;

[UsedImplicitly]
public abstract class SharedUpgradeKitSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public sealed class UpgradeKitDoAfterEvent : DoAfterEvent
    {
        public UpgradeKitDoAfterEvent() { }

        public override DoAfterEvent Clone() => this;
    }
}
