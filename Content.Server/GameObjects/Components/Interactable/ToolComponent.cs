using System;
using System.Collections.Generic;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems;
using Mono.Cecil;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    public interface IToolComponent
    {
        ToolQuality Qualities { get; set; }
    }

    [RegisterComponent]
    [ComponentReference(typeof(IToolComponent))]
    public class ToolComponent : SharedToolComponent, IToolComponent
    {
        protected ToolQuality _qualities = ToolQuality.None;

        [ViewVariables]
        public override ToolQuality Qualities
        {
            get => _qualities;
            set
            {
                _qualities = value;
                Dirty();
            }
        }

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedModifier { get; set; } = 1;

        public string UseSound { get; set; }

        public string UseSoundCollection { get; set; }

        public void AddQuality(ToolQuality quality)
        {
            _qualities |= quality;
            Dirty();
        }

        public void RemoveQuality(ToolQuality quality)
        {
            _qualities &= ~quality;
            Dirty();
        }

        public bool HasQuality(ToolQuality quality)
        {
            return _qualities.HasFlag(quality);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "qualities",
                new List<ToolQuality>(),
                qualities => qualities.ForEach(AddQuality),
                () =>
                {
                    var qualities = new List<ToolQuality>();

                    foreach (ToolQuality quality in Enum.GetValues(typeof(ToolQuality)))
                    {
                        if ((_qualities & quality) != 0)
                        {
                            qualities.Add(quality);
                        }
                    }

                    return qualities;
                });

            serializer.DataField(this, mod => SpeedModifier, "speed", 1);
            serializer.DataField(this, use => UseSound, "useSound", string.Empty);
            serializer.DataField(this, collection => UseSoundCollection, "useSoundCollection", string.Empty);
        }

        public virtual bool UseTool(IEntity user, IEntity target, ToolQuality toolQualityNeeded)
        {
            if (!HasQuality(toolQualityNeeded) || !ActionBlockerSystem.CanInteract(user))
                return false;

            PlayUseSound();

            return true;
        }

        protected void PlaySoundCollection(string name, float volume=-5f)
        {
            var file = AudioHelpers.GetRandomFileFromSoundCollection(name);
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity(file, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
        }

        public void PlayUseSound(float volume=-5f)
        {
            if(string.IsNullOrEmpty(UseSoundCollection))
                EntitySystem.Get<AudioSystem>()
                    .PlayFromEntity(UseSound, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
            else
                PlaySoundCollection(UseSoundCollection, volume);
        }
    }
}
