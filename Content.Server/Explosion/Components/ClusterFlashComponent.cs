using System;
using System.Threading.Tasks;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Flash.Components;
using Content.Server.Throwing;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class ClusterFlashComponent : Component, IInteractUsing, IUse
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "ClusterFlash";

        private Container _grenadesContainer = default!;

        /// <summary>
        ///     What we fill our prototype with if we want to pre-spawn with grenades.
        /// </summary>
        [ViewVariables] [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _fillPrototype;

        /// <summary>
        ///     If we have a pre-fill how many more can we spawn.
        /// </summary>
        private int _unspawnedCount;

        /// <summary>
        ///     Maximum grenades in the container.
        /// </summary>
        [ViewVariables] [DataField("maxGrenadesCount")]
        private int _maxGrenades = 3;

        /// <summary>
        ///     How long until our grenades are shot out and armed.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("delay")]
        private float _delay = 1;

        /// <summary>
        ///     Max distance grenades can be thrown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("distance")]
        private float _throwDistance = 50;

        /// <summary>
        ///     This is the end.
        /// </summary>
        private bool _countDown;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (_grenadesContainer.ContainedEntities.Count >= _maxGrenades ||
                !_entMan.HasComponent<FlashOnTriggerComponent>(args.Using))
                return false;

            _grenadesContainer.Insert(args.Using);
            UpdateAppearance();
            return true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _grenadesContainer = Owner.EnsureContainer<Container>("cluster-flash");

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
                if (_entMan.Deleted(Owner))
                    return;
                _countDown = true;
                var random = IoCManager.Resolve<IRobustRandom>();
                var delay = 20;
                var grenadesInserted = _grenadesContainer.ContainedEntities.Count + _unspawnedCount;
                var thrownCount = 0;
                var segmentAngle = 360 / grenadesInserted;
                while (TryGetGrenade(out var grenade))
                {
                    var angleMin = segmentAngle * thrownCount;
                    var angleMax = segmentAngle * (thrownCount + 1);
                    var angle = Angle.FromDegrees(random.Next(angleMin, angleMax));
                    // var distance = random.NextFloat() * _throwDistance;

                    delay += random.Next(550, 900);
                    thrownCount++;

                    // TODO: Suss out throw strength
                    grenade.TryThrow(angle.ToVec().Normalized * _throwDistance);

                    grenade.SpawnTimer(delay, () =>
                    {
                        if ((!_entMan.EntityExists(grenade) ? EntityLifeStage.Deleted : _entMan.GetComponent<MetaDataComponent>(grenade).EntityLifeStage) >= EntityLifeStage.Deleted)
                            return;

                        EntitySystem.Get<TriggerSystem>().Trigger(grenade, eventArgs.User);
                    });
                }

                _entMan.DeleteEntity(Owner);
            });
            return true;
        }

        private bool TryGetGrenade(out EntityUid grenade)
        {
            grenade = default;

            if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                grenade = _entMan.SpawnEntity(_fillPrototype, _entMan.GetComponent<TransformComponent>(Owner).MapPosition);
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
            if (!_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance)) return;

            appearance.SetData(ClusterFlashVisuals.GrenadesCounter, _grenadesContainer.ContainedEntities.Count + _unspawnedCount);
        }
    }
}
