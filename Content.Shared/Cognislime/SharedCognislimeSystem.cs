using Content.Shared.DoAfter;
using Robust.Shared.Serialization;


namespace Content.Shared.Cognislime;

/// <summary>
/// Makes objects sentient.
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
