using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Camera;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Storage.Components;
using Content.Server.Stunnable.Components;
using Content.Shared.Interaction;
using Content.Shared.PneumaticCannon;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Content.Server.Throwing;
using Content.Server.Tools.Components;
using Content.Shared.Sound;
using Content.Shared.Tool;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.PneumaticCannon
{
    public class PneumaticCannonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private Queue<FireData> _fireQueue = new();
        private float _accumulatedFrametime = 0f;
        private float _dequeueInterval = .1f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PneumaticCannonComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PneumaticCannonComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PneumaticCannonComponent, AfterInteractEvent>(OnAfterInteract);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_fireQueue.Count == 0)
            {
                _accumulatedFrametime = 0;
                return;
            }

            _accumulatedFrametime += frameTime;

            if (_accumulatedFrametime >= _dequeueInterval)
            {
                _accumulatedFrametime -= _dequeueInterval;
                var dat = _fireQueue.Dequeue();
                SoundSystem.Play(Filter.Pvs(dat.User), dat.Sound.GetSound());
                if (dat.User.TryGetComponent<CameraRecoilComponent>(out var recoil))
                {
                    recoil.Kick(Vector2.One * dat.Strength);
                }

                dat.Item.TryThrow(dat.Direction, dat.Strength, dat.User);
            }
        }

        private void OnComponentInit(EntityUid uid, PneumaticCannonComponent component, ComponentInit args)
        {
            component.GasTankSlot = component.Owner.EnsureContainer<ContainerSlot>($"{component.Name}-gasTank");
        }

        private void OnInteractUsing(EntityUid uid, PneumaticCannonComponent component, InteractUsingEvent args)
        {
            args.Handled = true;
            if (args.Used.HasComponent<GasTankComponent>() && component.GasTankSlot.CanInsert(args.Used))
            {
                component.GasTankSlot.Insert(args.Used);
                // popup
                UpdateAppearance(component);
                return;
            }

            if (args.Used.TryGetComponent<ToolComponent>(out var tool))
            {
                if (tool.HasQuality(component.ModifyMode))
                {
                    // this is kind of ugly but it just cycles the enum
                    var val = (byte) component.Mode;
                    val = (byte) ((val + 1) % Enum.GetValues<PneumaticCannonFireMode>().Length);
                    component.Mode = (PneumaticCannonFireMode) val;
                    // popup
                    // sound
                    return;
                } else if (tool.HasQuality(component.ModifyPower))
                {
                    var val = (byte) component.Power;
                    val = (byte) ((val + 1) % Enum.GetValues<PneumaticCannonPower>().Length);
                    component.Power = (PneumaticCannonPower) val;
                    // popup
                    // sound
                    return;
                }
            }

            if (args.Used.TryGetComponent<ItemComponent>(out var item)
                && component.Owner.TryGetComponent<ServerStorageComponent>(out var storage))
            {
                if (storage.CanInsert(args.Used))
                {
                    storage.Insert(args.Used);
                    // popup
                }
                else
                {
                    // popup
                }
            }
        }

        private void OnAfterInteract(EntityUid uid, PneumaticCannonComponent component, AfterInteractEvent args)
        {
            Fire(component, args.User, args.ClickLocation);
        }

        public void Fire(PneumaticCannonComponent component, IEntity user, EntityCoordinates click)
        {
            if (component.Owner.TryGetComponent<ServerStorageComponent>(out var storage))
            {
                if (storage.StoredEntities == null) return;
                if (storage.StoredEntities.Count == 0) return;
                List<IEntity> ents = new List<IEntity>();
                switch (component.Mode)
                {
                    case PneumaticCannonFireMode.Single:
                        ents.Add(_random.Pick(storage.StoredEntities));
                        break;
                    case PneumaticCannonFireMode.All:
                        ents = storage.StoredEntities.ToList();
                        break;

                }
                foreach (var entity in ents)
                {
                    storage.Remove(entity);
                    var dir = (click.ToMapPos(EntityManager) - user.Transform.WorldPosition).Normalized;

                    var randomAngle = GetRandomFireAngleFromPower(component.Power).RotateVec(dir);
                    var randomStrengthMult = _random.NextFloat(0.75f, 1.25f);
                    var throwMult = GetRangeMultFromPower(component.Power);

                    var data = new FireData
                    {
                        User = user,
                        Item = entity,
                        Strength = component.ThrowStrength * randomStrengthMult,
                        Sound = component.FireSound,
                        Direction = (dir + randomAngle).Normalized * component.BaseThrowRange * throwMult,
                    };
                    _fireQueue.Enqueue(data);
                }

                if (component.Power == PneumaticCannonPower.High)
                {
                    // if power was high, fall on our ass
                    if (user.TryGetComponent<StunnableComponent>(out var stun))
                    {
                        stun.Paralyze(3);
                        // popup
                    }
                }
            }
        }

        public void TryRemoveGasTank(PneumaticCannonComponent component, IEntity user)
        {
            if (component.GasTankSlot.ContainedEntity == null)
            {
                //popup
                return;
            }

            var ent = component.GasTankSlot.ContainedEntity;
            if (component.GasTankSlot.Remove(ent))
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    hands.TryPutInActiveHandOrAny(ent);
                }

                //popup
                UpdateAppearance(component);
            }
        }

        public void TryEjectAllItems(PneumaticCannonComponent component, IEntity user)
        {
            if (component.Owner.TryGetComponent<ServerStorageComponent>(out var storage))
            {
                if (storage.StoredEntities == null) return;
                foreach (var entity in storage.StoredEntities.ToArray())
                {
                    storage.Remove(entity);
                    if (user.TryGetComponent<HandsComponent>(out var hands))
                    {
                        hands.TryPutInActiveHandOrAny(entity);
                    }
                }
            }
        }

        private void UpdateAppearance(PneumaticCannonComponent component)
        {
            if (component.Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                appearance.SetData(PneumaticCannonVisuals.Tank,
                    component.GasTankSlot.ContainedEntities.Count != 0);
            }
        }

        private Angle GetRandomFireAngleFromPower(PneumaticCannonPower power)
        {
            switch (power)
            {
                default:
                case PneumaticCannonPower.Low:
                    return _random.NextAngle(-0.1, 0.1);
                case PneumaticCannonPower.Medium:
                    return _random.NextAngle(-0.2, 0.2);
                case PneumaticCannonPower.High:
                    return _random.NextAngle(-0.3, 0.3);
            }
        }

        private float GetRangeMultFromPower(PneumaticCannonPower power)
        {
            switch (power)
            {
                default:
                case PneumaticCannonPower.Low:
                    return 1.0f;
                case PneumaticCannonPower.Medium:
                    return 1.3f;
                case PneumaticCannonPower.High:
                    return 1.6f;
            }
        }

        public struct FireData
        {
            public IEntity User;
            public IEntity Item;
            public float Strength;
            public SoundSpecifier Sound;
            public Vector2 Direction;
        }
    }
}
