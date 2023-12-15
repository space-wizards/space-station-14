using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
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
