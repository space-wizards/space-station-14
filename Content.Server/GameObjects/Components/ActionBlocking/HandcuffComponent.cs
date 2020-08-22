
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Content.Server.GameObjects.Components.GUI;
using Robust.Shared.Serialization;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using Robust.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.ActionBlocking;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class HandcuffComponent : SharedHandcuffComponent, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
#pragma warning restore 649

        private const string FALLBACK_CUFF_PROTOTYPE = "Handcuffs";

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables]
        private float _cuffTime;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        private float _uncuffTime;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        private float _breakoutTime;

        /// <summary>
        ///     Override the prototype that gets spawned when the cuffs are removed from an entity.
        ///     This is useful for situations where you want to make handcuffs single-use (ie. zipties).
        ///     Leave this value empty to make the cuffs behave normally.
        /// </summary>
        [ViewVariables]
        private string _prototypeOverride = string.Empty;

        private float _interactRange;
        private DoAfterSystem _doAfterSystem;
        private AudioSystem _audioSystem;
        private string _startCuffSound = default;
        private string _endCuffSound = default;
        private string _startUncuffSound = default;
        private string _startBreakoutSound = default;
        private string _endUncuffSound = default;
        private string _cuffedTexture = default;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
            _doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            _interactRange = SharedInteractionSystem.InteractionRange / 2;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _cuffTime, "cuffTime", 5.0f);
            serializer.DataField(ref _breakoutTime, "breakoutTime", 30.0f);
            serializer.DataField(ref _uncuffTime, "uncuffTime", 5.0f);
            serializer.DataField(ref _startCuffSound, "startCuffSound", "/Audio/Items/Handcuffs/cuff_start.ogg");
            serializer.DataField(ref _endCuffSound, "endCuffSound", "/Audio/Items/Handcuffs/cuff_end.ogg");
            serializer.DataField(ref _startUncuffSound, "startUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");
            serializer.DataField(ref _endUncuffSound, "endUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
            serializer.DataField(ref _startBreakoutSound, "startBreakoutSound", "/Audio/Items/Handcuffs/cuff_breakout_start.ogg");
            serializer.DataField(ref _cuffedTexture, "cuffedTexture", "/Textures/Objects/Misc/cuff.png"); // this should probably be an RSI
            serializer.DataField(ref _prototypeOverride, "prototypeOverride", string.Empty);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User))
            {
                return;
            }

            if (eventArgs.Target == eventArgs.User)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, "You can't cuff yourself!");
                return;
            }

            if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var hands))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"{eventArgs.Target.Name} has no hands!");
                return;
            }

            if (eventArgs.Target.TryGetComponent<CuffedComponent>(out var cuffed) && cuffed.CuffedHandCount == hands.Count)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"{eventArgs.Target.Name} does not have any free hands to handcuff!");
                return;
            }

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(
                    eventArgs.User.Transform.MapPosition,
                    eventArgs.Target.Transform.MapPosition,
                    _interactRange,
                    ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, "You are too far away to use the cuffs!");
                return;
            }

            _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"You start cuffing {eventArgs.Target.Name}.");
            _notifyManager.PopupMessage(eventArgs.User, eventArgs.Target, $"{eventArgs.User.Name} starts cuffing you!");
            _audioSystem.PlayFromEntity(_startCuffSound, Owner);

            if (cuffed != null)
            {
                TryUpdateCuff(eventArgs.User, eventArgs.Target, cuffed); // add a new set of cuffs to an existing component
            }
            else
            {
                TryAddCuff(eventArgs.User, eventArgs.Target); // component doesn't exist yet, so add it
            }
        }

        // User has existing CuffedComponent so we add a new cuff entry to it.
        private async void TryUpdateCuff(IEntity user, IEntity target, CuffedComponent cuffs)
        {
            var doAfterEventArgs = new DoAfterEventArgs(user, _cuffTime, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _audioSystem.PlayFromEntity(_endCuffSound, Owner);
                _notifyManager.PopupMessage(user, user, $"You successfully cuff {target.Name}.");
                _notifyManager.PopupMessage(target, target, $"You have been cuffed by {user.Name}!");

                var config = new CuffedComponent.CuffConfig()
                {
                    BreakoutTime = _breakoutTime,
                    UncuffTime = _uncuffTime,
                    CuffedTexture = _cuffedTexture,
                    StartUncuffSound = _startUncuffSound,
                    EndUncuffSound = _endUncuffSound,
                    BreakoutSound = _startBreakoutSound
                };

                if (_prototypeOverride != string.Empty)
                {
                    config.PrototypeId = _prototypeOverride;
                    cuffs.AddNewCuffs(config);
                }
                else if (Owner.Prototype != null)
                {
                    config.PrototypeId = Owner.Prototype.ID;
                    cuffs.AddNewCuffs(config);
                }
                else // if the cuffs have no prototype we need to use a fallback value. 
                {
                    config.PrototypeId = FALLBACK_CUFF_PROTOTYPE;
                    cuffs.AddNewCuffs(config);
                    Logger.Warning($"Handcuff entity {Owner.Name} has no prototype!");
                }

                Owner.Delete();
            }
            else
            {
                _notifyManager.PopupMessage(user, user, $"You fail to cuff {target.Name}!");
                _notifyManager.PopupMessage(target, target, $"You interrupt {user.Name} while they are cuffing you!");
            }
        }

        // User has no CuffedComponent yet so we add one.
        private async void TryAddCuff(IEntity user, IEntity target)
        {
            var doAfterEventArgs = new DoAfterEventArgs(user, _cuffTime, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _audioSystem.PlayFromEntity(_endCuffSound, Owner);
                _notifyManager.PopupMessage(user, user, $"You successfully cuff {target.Name}.");
                _notifyManager.PopupMessage(target, target, $"You have been cuffed by {user.Name}!");

                var cuffs = target.AddComponent<CuffedComponent>();
                var config = new CuffedComponent.CuffConfig()
                {
                    BreakoutTime = _breakoutTime,
                    UncuffTime = _uncuffTime,
                    CuffedTexture = _cuffedTexture,
                    StartUncuffSound = _startUncuffSound,
                    EndUncuffSound = _endUncuffSound,
                    BreakoutSound = _startBreakoutSound
                };

                if (_prototypeOverride != string.Empty)
                {
                    config.PrototypeId = _prototypeOverride;
                    cuffs.AddNewCuffs(config);
                }
                else if (Owner.Prototype != null)
                {
                    config.PrototypeId = Owner.Prototype.ID;
                    cuffs.AddNewCuffs(config);
                }
                else // if the cuffs have no prototype we need to use a fallback value. this shouldn't happen but lets be careful.
                {
                    config.PrototypeId = FALLBACK_CUFF_PROTOTYPE;
                    cuffs.AddNewCuffs(config);
                    Logger.Warning($"Handcuff entity {Owner.Name} has no prototype!");
                }

                Owner.Delete();
            }
            else
            {
                _notifyManager.PopupMessage(user, user, $"You fail to cuff {target.Name}!");
                _notifyManager.PopupMessage(target, target, $"You interrupt {user.Name} while they are cuffing you!");
            }  
        }
    }
}
