using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
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
        [DataField("qualities")]
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
        [DataField("speed")]
        public float SpeedModifier { get; set; } = 1;

        [DataField("useSound")]
        public string UseSound { get; set; }

        [DataField("useSoundCollection")]
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

        public virtual async Task<bool> UseTool(IEntity user, IEntity target, float doAfterDelay, ToolQuality toolQualityNeeded, Func<bool> doAfterCheck = null)
        {
            if (!HasQuality(toolQualityNeeded) || !ActionBlockerSystem.CanInteract(user))
                return false;

            if (doAfterDelay > 0f)
            {
                var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                var doAfterArgs = new DoAfterEventArgs(user, doAfterDelay / SpeedModifier, default, target)
                {
                    ExtraCheck = doAfterCheck,
                    BreakOnDamage = false, // TODO: Change this to true once breathing is fixed.
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    NeedHand = true,
                };

                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result == DoAfterStatus.Cancelled)
                    return false;
            }

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
            if (string.IsNullOrEmpty(UseSoundCollection))
            {
                if (!string.IsNullOrEmpty(UseSound))
                {
                    EntitySystem.Get<AudioSystem>()
                        .PlayFromEntity(UseSound, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
                }
            }
            else
            {
                PlaySoundCollection(UseSoundCollection, volume);
            }
        }
    }
}
