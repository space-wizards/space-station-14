using Content.Shared.DoAfter;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Cognislime;

/// <summary>
/// Makes stuff sentient.
/// </summary>
public abstract class SharedCognislimeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    [Serializable, NetSerializable]
    public sealed partial class CognislimeDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
