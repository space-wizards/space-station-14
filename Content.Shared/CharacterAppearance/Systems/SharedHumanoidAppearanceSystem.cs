using System;
using Content.Shared.Body.Part;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Log;

namespace Content.Shared.CharacterAppearance.Systems
{
    public abstract class SharedHumanoidAppearanceSystem : EntitySystem
    {
        /*
        public override void Initialize()
        {
            SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentInit>(OnHumanoidAppearanceInit);
        }

        private void OnHumanoidAppearanceInit(EntityUid uid, HumanoidAppearanceComponent component, ComponentInit _)
        {
            // we tell the server that a new apperance component exists
            // So this works, but it *works*. Every time a component is created,
            // even if it's a client-side component, this is called.
            // This means that it runs at Theta((player count * 2) - 1),
            // echoing every single time the client inits the component
            //
            // For obvious reasons, this is not ideal. This should run
            // at O(player count - 1) at the worst.
            RaiseNetworkEvent(new HumanoidAppearanceComponentInitEvent(uid));
        }
        */

        // *points at systems* you motherfuckers update your Own God Damn Profiles
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
        public class HumanoidAppearanceProfileChangedEvent : EntityEventArgs
        {
            public EntityUid Uid { get; }
            public HumanoidCharacterAppearance Appearance { get; }
            public Sex Sex { get; }
            public Gender Gender { get; }

            public HumanoidAppearanceProfileChangedEvent(EntityUid uid, HumanoidCharacterProfile profile)
            {
                Uid = uid;
                Appearance = profile.Appearance;
                Sex = profile.Sex;
                Gender = profile.Gender;
            }

            public HumanoidAppearanceProfileChangedEvent(EntityUid uid, HumanoidCharacterAppearance appearance, Sex sex, Gender gender)
            {
                Uid = uid;
                Appearance = appearance;
                Sex = sex;
                Gender = gender;
            }
        }
    }
}
