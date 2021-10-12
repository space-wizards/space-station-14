using System;
using Content.Shared.Body.Part;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance.Systems
{
    public class SharedHumanoidAppearanceSystem : EntitySystem
    {
        public void UpdateFromProfile(EntityUid uid, ICharacterProfile profile)
        {
            if (!EntityManager.GetEntity(uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            var humanoid = (HumanoidCharacterProfile) profile;
            component.Appearance = humanoid.Appearance;
            component.Sex = humanoid.Sex;
            component.Gender = humanoid.Gender;
            RaiseLocalEvent(new HumanoidAppearanceProfileChangedEvent(uid, humanoid));
            RaiseNetworkEvent(new HumanoidAppearanceProfileChangedEvent(uid, humanoid));
        }

        // Scaffolding until Body is moved to ECS.
        public void BodyPartAdded(EntityUid uid, BodyPartAddedEventArgs args)
        {
            RaiseNetworkEvent(new HumanoidAppearanceBodyPartAddedEvent(uid, args));
        }

        public void BodyPartRemoved(EntityUid uid, BodyPartRemovedEventArgs args)
        {
            RaiseNetworkEvent(new HumanoidAppearanceBodyPartRemovedEvent(uid, args));
        }
    }

    // Scaffolding until Body is moved to ECS.
    [Serializable, NetSerializable]
    public class HumanoidAppearanceBodyPartAddedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; }
        public BodyPartAddedEventArgs Args { get; }

        public HumanoidAppearanceBodyPartAddedEvent(EntityUid uid, BodyPartAddedEventArgs args)
        {
            Args = args;
        }
    }

    [Serializable, NetSerializable]
    public class HumanoidAppearanceBodyPartRemovedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; }
        public BodyPartRemovedEventArgs Args { get; }

        public HumanoidAppearanceBodyPartRemovedEvent(EntityUid uid, BodyPartRemovedEventArgs args)
        {
            Args = args;
        }

    }

    [Serializable, NetSerializable]
    public class HumanoidAppearanceProfileChangedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; }
        public HumanoidCharacterProfile Profile { get; }
        public HumanoidAppearanceProfileChangedEvent(EntityUid uid, HumanoidCharacterProfile profile)
        {
            Uid = uid;
            Profile = profile;
        }
    }
}
