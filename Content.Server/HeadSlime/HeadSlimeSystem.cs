using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Cloning;
using Content.Server.Drone.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.Components;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.HeadSlime;
using Robust.Shared.Prototypes;
using Content.Server.Actions;
using Content.Shared.Actions;
using Content.Shared.StatusEffect;
using Content.Server.Medical;
using Content.Shared.Audio;

namespace Content.Server.HeadSlime
{
    public sealed partial class HeadSlimeSystem : SharedHeadSlimeSystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        
        private const string StatusEffectKey = "ForcedSleep"; // For the Inject Action.

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HeadSlimeComponent, CloningEvent>(OnHeadSlimeCloning);

            SubscribeLocalEvent<HeadSlimeComponent, HeadSlimeInfectDoAfterEvent>(InfectOnDoAfter);
            SubscribeLocalEvent<HeadSlimeComponent, HeadSlimeInjectDoAfterEvent>(InjectOnDoAfter);
        }

        /// <summary>
        ///     Turns the target into a Head Slime Antag.
        /// </summary>
        private void InfectOnDoAfter(EntityUid uid, HeadSlimeComponent component, HeadSlimeInfectDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (TryComp(args.Args.Target, out MobStateComponent? targetState))
            {
                if (targetState.CurrentState == MobState.Dead)
                {
                    _popup.PopupEntity(Loc.GetString("Head-Slime-action-popup-message-fail-target-dead"), uid, uid);
                    return;
                }
            }

            if (args.Args.Target != null)
            {
                var subject = args.Args.Target.Value;

                //Remove any hats the target is wearing, for ease of use
                if (_inventory.TryGetSlotEntity(args.Args.Target.Value, "head", out var _))
                {
                    _inventory.TryUnequip(args.Args.Target.Value, "head");
                }

                HeadSlimeEntity(subject);
            }
            
        }

        /// <summary>
        ///     Puts the taget of the Inject action to sleep.
        /// </summary>
        private void InjectOnDoAfter(EntityUid uid, HeadSlimeComponent component, HeadSlimeInjectDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (TryComp(args.Args.Target, out MobStateComponent? targetState))
            {
                if (targetState.CurrentState == MobState.Dead)
                {
                    _popup.PopupEntity(Loc.GetString("Head-Slime-action-popup-message-fail-target-dead"), uid, uid);
                    return;
                }
            }

            if (args.Args.Target != null)
            {
                var subject = args.Args.Target.Value;            
                
                _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(subject, StatusEffectKey,
                TimeSpan.FromSeconds(15), false);
            }
        }

        private void OnHeadSlimeCloning(EntityUid uid, HeadSlimeComponent HeadSlimecomp, ref CloningEvent args)
        {
            if (UnHeadSlime(args.Source, args.Target, HeadSlimecomp))
                args.NameHandled = true;
        }
    }

}
