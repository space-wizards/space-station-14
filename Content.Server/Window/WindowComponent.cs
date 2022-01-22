using System;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Rounding;
using Content.Shared.Sound;
using Content.Shared.Window;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Window
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
#pragma warning disable 618
    public class WindowComponent : SharedWindowComponent, IExamine, IInteractHand
#pragma warning restore 618
    {
        [ViewVariables(VVAccess.ReadWrite)] private TimeSpan _lastKnockTime;

        [DataField("knockDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan KnockDelay = TimeSpan.FromSeconds(0.5);

        [DataField("rateLimitedKnocking")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool RateLimitedKnocking = true;

        [DataField("knockSound")]
        public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Effects/glass_knock.ogg");

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            /*if (RateLimitedKnocking && _gameTiming.CurTime < _lastKnockTime + KnockDelay)
            {
                return false;
            }

            SoundSystem.Play(
                Filter.Pvs(eventArgs.Target), KnockSound.GetSound(),
                _entMan.GetComponent<TransformComponent>(eventArgs.Target).Coordinates, AudioHelpers.WithVariation(0.05f));
            eventArgs.Target.PopupMessageEveryone(Loc.GetString("comp-window-knock"));

            _lastKnockTime = _gameTiming.CurTime;*/

            return true;
        }
    }
}
