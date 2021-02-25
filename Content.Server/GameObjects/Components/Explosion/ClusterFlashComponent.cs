#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.Trigger.TimerTrigger;
using Content.Shared.GameObjects.Components.Explosion;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Explosion
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
        ///     If we have a pre-fill how many more can we spawn.
        /// </summary>
        private int _unspawnedCount;

        /// <summary>
        ///     Maximum grenades in the container.
        /// </summary>
        [ViewVariables]
        private int _maxGrenades;

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

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (_grenadesContainer.ContainedEntities.Count >= _maxGrenades || !args.Using.HasComponent<FlashExplosiveComponent>())
                return false;

            _grenadesContainer.Insert(args.Using);
            UpdateAppearance();
            return true;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fillPrototype, "fillPrototype", null);
            serializer.DataField(ref _maxGrenades, "maxGrenadesCount", 3);
            serializer.DataField(ref _delay, "delay", 1.0f);
            serializer.DataField(ref _throwDistance, "distance", 3.0f);
        }

        public override void Initialize()
        {
            base.Initialize();

            _grenadesContainer = ContainerManagerComponent.Ensure<Container>("cluster-flash", Owner);

        }

        protected override void Startup()
        {
            base.Startup();

            if (_fillPrototype != null)
            {
                _unspawnedCount = Math.Max(0, _maxGrenades - _grenadesContainer.ContainedEntities.Count);
                UpdateAppearance();
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (_countDown || (_grenadesContainer.ContainedEntities.Count + _unspawnedCount) <= 0)
                return false;
            Owner.SpawnTimer((int) (_delay * 1000), () =>
            {
                if (Owner.Deleted)
                    return;
                _countDown = true;
                var random = IoCManager.Resolve<IRobustRandom>();
                var delay = 20;
                var grenadesInserted = _grenadesContainer.ContainedEntities.Count + _unspawnedCount;
                var thrownCount = 0;
                var segmentAngle = (int) (360 / grenadesInserted);
                while (TryGetGrenade(out var grenade))
                {
                    var angleMin = segmentAngle * thrownCount;
                    var angleMax = segmentAngle * (thrownCount + 1);
                    var angle = Angle.FromDegrees(random.Next(angleMin, angleMax));
                    // var distance = random.NextFloat() * _throwDistance;

                    delay += random.Next(550, 900);
                    thrownCount++;

                    // TODO: Suss out throw strength
                    grenade.TryThrow(angle.ToVec().Normalized * 50);

                    grenade.SpawnTimer(delay, () =>
                    {
                        if (grenade.Deleted)
                            return;

                        if (grenade.TryGetComponent(out OnUseTimerTriggerComponent? useTimer))
                        {
                            useTimer.Trigger(eventArgs.User);
                        }
                    });
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

            appearance.SetData(ClusterFlashVisuals.GrenadesCounter, _grenadesContainer.ContainedEntities.Count + _unspawnedCount);
        }
    }
}
