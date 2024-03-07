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

    public sealed class AddCognislimeDoAfterEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Target;
        public readonly EntityUid Cognislime;

        public AddCognislimeDoAfterEvent(EntityUid user, EntityUid target, EntityUid cognislime)
        {
            User = user;
            Target = target;
            Cognislime = cognislime;
        }
    }
}
