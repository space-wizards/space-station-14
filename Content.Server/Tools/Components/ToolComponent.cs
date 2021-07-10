using System;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Interaction.Events;
using Content.Shared.Sound;
using Content.Shared.Tool;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Tools.Components
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
        public SoundSpecifier UseSound { get; set; } = default!;

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

        public virtual async Task<bool> UseTool(IEntity user, IEntity? target, float doAfterDelay, ToolQuality toolQualityNeeded, Func<bool>? doAfterCheck = null)
        {
            if (!HasQuality(toolQualityNeeded) || !EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
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

                var result = await doAfterSystem.WaitDoAfter(doAfterArgs);

                if (result == DoAfterStatus.Cancelled)
                    return false;
            }

            PlayUseSound();

            return true;
        }

        public void PlayUseSound(float volume = -5f)
        {
            if(UseSound.TryGetSound(out var useSound))
                SoundSystem.Play(Filter.Pvs(Owner), useSound, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
        }
    }
}
