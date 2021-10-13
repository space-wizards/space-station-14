using System;
using Content.Shared.Body.Part;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance.Systems
{
    public abstract class SharedHumanoidAppearanceSystem : EntitySystem
    {
        // I tried to make this a shared class - apparently,
        // updating components doesn't exactly work in the
        // shared namespace? It's an abstract method for now,
        // until somebody can fix this.
        public abstract void OnAppearanceChange(ChangedHumanoidAppearanceEvent args);

        public abstract void UpdateFromProfile(EntityUid uid, ICharacterProfile profile);

        // Scaffolding until Body is moved to ECS.
        public void BodyPartAdded(EntityUid uid, BodyPartAddedEventArgs args)
        {
            RaiseNetworkEvent(new HumanoidAppearanceBodyPartAddedEvent(uid, args));
        }

        public void BodyPartRemoved(EntityUid uid, BodyPartRemovedEventArgs args)
        {
            RaiseNetworkEvent(new HumanoidAppearanceBodyPartRemovedEvent(uid, args));
        }

        // Scaffolding until Body is moved to ECS.
        [Serializable, NetSerializable]
        public class HumanoidAppearanceBodyPartAddedEvent : EntityEventArgs
        {
            public EntityUid Uid { get; }
            public BodyPartAddedEventArgs Args { get; }

            public HumanoidAppearanceBodyPartAddedEvent(EntityUid uid, BodyPartAddedEventArgs args)
            {
                Uid = uid;
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
                Uid = uid;
                Args = args;
            }

        }

        [Serializable, NetSerializable]
        public class HumanoidAppearanceComponentInitEvent : EntityEventArgs
        {
            public EntityUid Uid { get; }

            public HumanoidAppearanceComponentInitEvent(EntityUid uid)
            {
                Uid = uid;
            }
        }

        [Serializable, NetSerializable]
        public class ChangedHumanoidAppearanceEvent : EntityEventArgs
        {
            public EntityUid Uid { get; }
            public HumanoidCharacterAppearance Appearance { get; }
            public Sex Sex { get; }
            public Gender Gender { get; }

            public ChangedHumanoidAppearanceEvent(EntityUid uid, HumanoidCharacterProfile profile)
            {
                Uid = uid;
                Appearance = profile.Appearance;
                Sex = profile.Sex;
                Gender = profile.Gender;
            }

            public ChangedHumanoidAppearanceEvent(EntityUid uid, HumanoidCharacterAppearance appearance, Sex sex, Gender gender)
            {
                Uid = uid;
                Appearance = appearance;
                Sex = sex;
                Gender = gender;
            }
        }
    }
}
