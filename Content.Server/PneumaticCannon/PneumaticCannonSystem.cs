using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Camera;
using Content.Server.Nutrition.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.Stunnable;
using Content.Shared.Camera;
using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.PneumaticCannon;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.PneumaticCannon
{
    public sealed class PneumaticCannonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;
        [Dependency] private readonly GasTankSystem _gasTank = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

        private HashSet<PneumaticCannonComponent> _currentlyFiring = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PneumaticCannonComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PneumaticCannonComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PneumaticCannonComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<PneumaticCannonComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerbs);
            SubscribeLocalEvent<PneumaticCannonComponent, GetVerbsEvent<Verb>>(OnOtherVerbs);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_currentlyFiring.Count == 0)
                return;

            foreach (var comp in _currentlyFiring.ToArray())
            {
                if (comp.FireQueue.Count == 0)
                {
                    _currentlyFiring.Remove(comp);
                    // reset acc frametime to the fire interval if we're instant firing
                    if (comp.InstantFire)
                    {
                        comp.AccumulatedFrametime = comp.FireInterval;
                    }
                    else
                    {
                        comp.AccumulatedFrametime = 0f;
                    }
                    return;
                }

                comp.AccumulatedFrametime += frameTime;
                if (comp.AccumulatedFrametime > comp.FireInterval)
                {
                    var dat = comp.FireQueue.Dequeue();
                    Fire(comp, dat);
                    comp.AccumulatedFrametime -= comp.FireInterval;
                }
            }
        }

        private void OnComponentInit(EntityUid uid, PneumaticCannonComponent component, ComponentInit args)
        {
            component.GasTankSlot = component.Owner.EnsureContainer<ContainerSlot>($"{component.Name}-gasTank");

            if (component.InstantFire)
                component.AccumulatedFrametime = component.FireInterval;
        }

        private void OnInteractUsing(EntityUid uid, PneumaticCannonComponent component, InteractUsingEvent args)
        {
            args.Handled = true;
            if (EntityManager.HasComponent<GasTankComponent>(args.Used)
                && component.GasTankSlot.CanInsert(args.Used)
                && component.GasTankRequired)
            {
                component.GasTankSlot.Insert(args.Used);
                args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-gas-tank-insert",
                    ("tank", args.Used), ("cannon", component.Owner)));
                UpdateAppearance(component);
                return;
            }

            if (EntityManager.TryGetComponent<ToolComponent?>(args.Used, out var tool))
            {
                if (tool.Qualities.Contains(component.ToolModifyMode))
                {
                    // this is kind of ugly but it just cycles the enum
                    var val = (int) component.Mode;
                    val = (val + 1) % (int) PneumaticCannonFireMode.Len;
                    component.Mode = (PneumaticCannonFireMode) val;
                    args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-change-fire-mode",
                        ("mode", component.Mode.ToString())));
                    // sound
                    return;
                }

                if (tool.Qualities.Contains(component.ToolModifyPower))
                {
                    var val = (int) component.Power;
                    val = (val + 1) % (int) PneumaticCannonPower.Len;
                    component.Power = (PneumaticCannonPower) val;
                    args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-change-power",
                        ("power", component.Power.ToString())));
                    // sound
                    return;
                }
            }

            // this overrides the ServerStorageComponent's insertion stuff because
            // it's not event-based yet and I can't cancel it, so tools and stuff
            // will modify mode/power then get put in anyway
            if (EntityManager.TryGetComponent<ItemComponent?>(args.Used, out var item)
                && EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
            {
                if (_storageSystem.CanInsert(component.Owner, args.Used, out _, storage))
                {
                    _storageSystem.Insert(component.Owner, args.Used, storage);
                    args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-insert-item-success",
                        ("item", args.Used), ("cannon", component.Owner)));
                }
                else
                {
                    args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-insert-item-failure",
                        ("item", args.Used), ("cannon", component.Owner)));
                }
            }
        }

        private void OnAfterInteract(EntityUid uid, PneumaticCannonComponent component, AfterInteractEvent args)
        {
            if (EntityManager.TryGetComponent<SharedCombatModeComponent>(uid, out var combat)
                && !combat.IsInCombatMode)
                return;

            args.Handled = true;

            if (!HasGas(component) && component.GasTankRequired)
            {
                args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-fire-no-gas",
                    ("cannon", component.Owner)));
                SoundSystem.Play("/Audio/Items/hiss.ogg", Filter.Pvs(args.Used), args.Used, AudioParams.Default);
                return;
            }
            AddToQueue(component, args.User, args.ClickLocation);
        }

        public void AddToQueue(PneumaticCannonComponent comp, EntityUid user, EntityCoordinates click)
        {
            if (!EntityManager.TryGetComponent<ServerStorageComponent?>(comp.Owner, out var storage))
                return;
            if (storage.StoredEntities == null) return;
            if (storage.StoredEntities.Count == 0)
            {
                SoundSystem.Play("/Audio/Weapons/click.ogg", Filter.Pvs((comp).Owner), ((IComponent) comp).Owner, AudioParams.Default);
                return;
            }

            _currentlyFiring.Add(comp);

            int entCounts = comp.Mode switch
            {
                PneumaticCannonFireMode.All => storage.StoredEntities.Count,
                PneumaticCannonFireMode.Single => 1,
                _ => 0
            };

            for (int i = 0; i < entCounts; i++)
            {
                var dir = (click.ToMapPos(EntityManager) - EntityManager.GetComponent<TransformComponent>(user).WorldPosition).Normalized;

                var randomAngle = GetRandomFireAngleFromPower(comp.Power).RotateVec(dir);
                var randomStrengthMult = _random.NextFloat(0.75f, 1.25f);
                var throwMult = GetRangeMultFromPower(comp.Power);

                var data = new PneumaticCannonComponent.FireData
                {
                    User = user,
                    Strength = comp.ThrowStrength * randomStrengthMult,
                    Direction = (dir + randomAngle).Normalized * comp.BaseThrowRange * throwMult,
                };
                comp.FireQueue.Enqueue(data);
            }
        }

        public void Fire(PneumaticCannonComponent comp, PneumaticCannonComponent.FireData data)
        {
            if (!HasGas(comp) && comp.GasTankRequired)
            {
                data.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-fire-no-gas",
                    ("cannon", comp.Owner)));
                SoundSystem.Play("/Audio/Items/hiss.ogg", Filter.Pvs(comp.Owner), comp.Owner, AudioParams.Default);
                return;
            }

            if (!EntityManager.TryGetComponent<ServerStorageComponent?>(comp.Owner, out var storage))
                return;

            if (Deleted(data.User))
                return;

            if (storage.StoredEntities == null) return;
            if (storage.StoredEntities.Count == 0) return; // click sound?

            var ent = _random.Pick(storage.StoredEntities);
            _storageSystem.RemoveAndDrop(comp.Owner, ent, storage);

            SoundSystem.Play(comp.FireSound.GetSound(), Filter.Pvs(data.User), comp.Owner, AudioParams.Default);
            if (EntityManager.HasComponent<CameraRecoilComponent>(data.User))
            {
                var kick = Vector2.One * data.Strength;
                _cameraRecoil.KickCamera(data.User, kick);
            }

            _throwingSystem.TryThrow(ent, data.Direction, data.Strength, data.User, GetPushbackRatioFromPower(comp.Power));

            if(EntityManager.TryGetComponent<StatusEffectsComponent?>(data.User, out var status)
               && comp.Power == PneumaticCannonPower.High)
            {
                _stun.TryParalyze(data.User, TimeSpan.FromSeconds(comp.HighPowerStunTime), true, status);

                data.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-power-stun",
                    ("cannon", comp.Owner)));
            }

            if (comp.GasTankSlot.ContainedEntity is {Valid: true} contained && comp.GasTankRequired)
            {
                // we checked for this earlier in HasGas so a GetComp is okay
                var gas = EntityManager.GetComponent<GasTankComponent>(contained);
                var environment = _atmos.GetContainingMixture(comp.Owner, false, true);
                var removed = _gasTank.RemoveAir(gas, GetMoleUsageFromPower(comp.Power));
                if (environment != null && removed != null)
                {
                    _atmos.Merge(environment, removed);
                }
            }
        }

        /// <summary>
        ///     Returns whether the pneumatic cannon has enough gas to shoot an item.
        /// </summary>
        public bool HasGas(PneumaticCannonComponent component)
        {
            var usage = GetMoleUsageFromPower(component.Power);

            if (component.GasTankSlot.ContainedEntity is not {Valid: true } contained)
                return false;

            // not sure how it wouldnt, but it might not! who knows
            if (EntityManager.TryGetComponent<GasTankComponent?>(contained, out var tank))
            {
                if (tank.Air.TotalMoles < usage)
                    return false;

                return true;
            }

            return false;
        }

        private void OnAlternativeVerbs(EntityUid uid, PneumaticCannonComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (component.GasTankSlot.ContainedEntities.Count == 0 || !component.GasTankRequired)
                return;
            if (!args.CanInteract)
                return;

            AlternativeVerb ejectTank = new();
            ejectTank.Act = () => TryRemoveGasTank(component, args.User);
            ejectTank.Text = Loc.GetString("pneumatic-cannon-component-verb-gas-tank-name");
            args.Verbs.Add(ejectTank);
        }

        private void OnOtherVerbs(EntityUid uid, PneumaticCannonComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanInteract)
                return;

            Verb ejectItems = new();
            ejectItems.Act = () => TryEjectAllItems(component, args.User);
            ejectItems.Text = Loc.GetString("pneumatic-cannon-component-verb-eject-items-name");
            ejectItems.DoContactInteraction = true;
            args.Verbs.Add(ejectItems);
        }

        public void TryRemoveGasTank(PneumaticCannonComponent component, EntityUid user)
        {
            if (component.GasTankSlot.ContainedEntity is not {Valid: true} contained)
            {
                user.PopupMessage(Loc.GetString("pneumatic-cannon-component-gas-tank-none",
                    ("cannon", component.Owner)));
                return;
            }

            if (component.GasTankSlot.Remove(contained))
            {
                _handsSystem.TryPickupAnyHand(user, contained);

                user.PopupMessage(Loc.GetString("pneumatic-cannon-component-gas-tank-remove",
                    ("tank", contained), ("cannon", component.Owner)));
                UpdateAppearance(component);
            }
        }

        public void TryEjectAllItems(PneumaticCannonComponent component, EntityUid user)
        {
            if (EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
            {
                if (storage.StoredEntities == null) return;
                foreach (var entity in storage.StoredEntities.ToArray())
                {
                    _storageSystem.RemoveAndDrop(component.Owner, entity, storage);
                }

                user.PopupMessage(Loc.GetString("pneumatic-cannon-component-ejected-all",
                    ("cannon", (component.Owner))));
            }
        }

        private void UpdateAppearance(PneumaticCannonComponent component)
        {
            if (EntityManager.TryGetComponent<AppearanceComponent?>(component.Owner, out var appearance))
            {
                appearance.SetData(PneumaticCannonVisuals.Tank,
                    component.GasTankSlot.ContainedEntities.Count != 0);
            }
        }

        private Angle GetRandomFireAngleFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.High => _random.NextAngle(-0.3, 0.3),
                PneumaticCannonPower.Medium => _random.NextAngle(-0.2, 0.2),
                PneumaticCannonPower.Low or _ => _random.NextAngle(-0.1, 0.1),
            };
        }

        private float GetRangeMultFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.High => 1.6f,
                PneumaticCannonPower.Medium => 1.3f,
                PneumaticCannonPower.Low or _ => 1.0f,
            };
        }

        private float GetMoleUsageFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.High => 9f,
                PneumaticCannonPower.Medium => 6f,
                PneumaticCannonPower.Low or _ => 3f,
            };
        }

        private float GetPushbackRatioFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.Medium => 8.0f,
                PneumaticCannonPower.High => 16.0f,
                PneumaticCannonPower.Low or _ => 0f
            };
        }
    }
}
