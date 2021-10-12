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
    public class SharedHumanoidAppearanceSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentInit>(OnHumanoidAppearanceInit);
        }

        private void OnHumanoidAppearanceInit(EntityUid uid, HumanoidAppearanceComponent component, ComponentInit _)
        {
            // we tell the server that a new apperance component exists
            RaiseNetworkEvent(new HumanoidAppearanceComponentInitEvent(uid));
        }


        public void UpdateFromProfile(EntityUid uid, ICharacterProfile profile)
        {
            if (!EntityManager.GetEntity(uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            var humanoid = (HumanoidCharacterProfile) profile;

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
