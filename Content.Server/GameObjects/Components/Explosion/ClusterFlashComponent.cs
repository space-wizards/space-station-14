#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Trigger.TimerTrigger;
using Content.Server.Throw;
using Robust.Server.GameObjects;
using Content.Shared.GameObjects.Components.Explosion;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Explosives
{
    [RegisterComponent]
    public sealed class ClusterFlashComponent : Component, IInteractUsing, IUse
    {
        public override string Name => "ClusterFlash";

        private Container _grenadesContainer = default!;

        /// <summary>
        ///     What we fill our prototype with if we want to pre-spawn with grenades.
        /// </summary>
        [ViewVariables]
        private string? _fillPrototype;

        /// <summary>
        ///     Maximum grenades in the container.
        /// </summary>
        [ViewVariables]
        private int _maxGrenades;

        /// <summary>
        ///     If we have a pre-fill how many more can we spawn.
        /// </summary>
        private byte _unspawnedCount;

        /// <summary>
        ///     How long until our grenades are shot out and armed.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _delay;

        /// <summary>
        ///     Max distance grenades can be thrown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _throwDistance;

        /// <summary>
        ///     This is the end.
        /// </summary>
        private bool _countDown;

        // I'm suss on this bit
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (_grenadesContainer.ContainedEntities.Count + _unspawnedCount >= _maxGrenades || !args.Using.HasComponent<FlashExplosiveComponent>())
                return false;

            _grenadesContainer.Insert(args.Using);
            UpdateAppearance();
            return true;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x._fillPrototype, "fillPrototype", null);
            serializer.DataField(this, x => x._maxGrenades, "maxGrenadesCount", 3);
            serializer.DataField(this, x => x._delay, "delay", 1.0f);
            serializer.DataField(this, x => x._throwDistance, "distance", 5.0f);
        }

        public override void Initialize()
        {
            base.Initialize();

            _grenadesContainer = ContainerManagerComponent.Ensure<Container>("cluster-flash", Owner);

            if (_fillPrototype != null)
                _unspawnedCount += (byte) Math.Max(0, _maxGrenades - _grenadesContainer.ContainedEntities.Count);

        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Owner.SpawnTimer((int) (_delay * 1000), () =>
            {
                if (Owner.Deleted || _countDown)
                    return;

                _countDown = true;
                var random = IoCManager.Resolve<IRobustRandom>();
                var worldPos = Owner.Transform.WorldPosition;
                var delay = 0;

                while (TryGetGrenade(out var grenade))
                {
                    // Okay ThrowHelper is actually disgusting and so is this
                    var angle = Angle.FromDegrees(random.Next(359));
                    var distance = random.Next() * _throwDistance;
                    var target = new EntityCoordinates(grenade.Uid, worldPos + angle.ToVec() * distance);

                    grenade.Throw(1f, target, Owner.Transform.Coordinates);

                    grenade.SpawnTimer(delay, () =>
                    {
                        if (grenade.Deleted)
                            return;

                        if (grenade.TryGetComponent(out OnUseTimerTriggerComponent? useTimer))
                        {
                            useTimer.Trigger(eventArgs.User);
                        }
                    });

                    delay += random.Next(100, 300);
                }

                Owner.Delete();
            });
            return true;
        }

        private bool TryGetGrenade([NotNullWhen(true)] out IEntity? grenade)
        {
            grenade = null;

            if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                grenade = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.MapPosition);
                return true;
            }

            if (_grenadesContainer.ContainedEntities.Count > 0)
            {
                grenade = _grenadesContainer.ContainedEntities[0];

                // This shouldn't happen but you never know.
                if (!_grenadesContainer.Remove(grenade))
                    return false;

                return true;
            }

            return false;
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance)) return;

            appearance.SetData(ClusterFlashVisuals.GrenadesCounter, _grenadesContainer.ContainedEntities.Count);
            appearance.SetData(ClusterFlashVisuals.GrenadesMax, _maxGrenades);
        }
    }
}
