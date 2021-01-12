using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class AreaEffectComponent : Component
    {
        public override string Name => "AreaEffect";
        public int Amount { get; set; }
        public AreaEffectInception Inception { get; set; }

        public void Start(int amount, float duration, float spreadDelay, float removeDelay)
        {
            if (Inception != null)
                return;

            Amount = amount;
            var inception = new AreaEffectInception(amount, duration, spreadDelay, removeDelay);

            inception.Add(this);

            // Register the inception into the AreaEffect system
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AreaEffectInceptionCreatedMessage(Inception));
        }

        public void Spread()
        {
            if (Owner.Prototype?.ID == null)
                return;

            if (!Owner.TryGetComponent(out SnapGridComponent snapGrid))
            {
                Logger.Error("AreaEffectComponent attached to "+Owner.Prototype?.ID+" couldn't get SnapGridComponent from owner.");
                return;
            }

            var spawned = new HashSet<IEntity>();
            void SpreadToDir(Direction dir)
            {
                foreach (var neighbor in snapGrid.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out AreaEffectComponent comp) && comp.Inception == Inception)
                        return;

                    if (neighbor.TryGetComponent(out AirtightComponent airtight) && airtight.AirBlocked)
                        return;
                }
                var newEffect = Owner.EntityManager.SpawnEntity(Owner.Prototype?.ID, snapGrid.DirectionToGrid(dir));
                if (!newEffect.TryGetComponent(out AreaEffectComponent effectComponent))
                {
                    newEffect.Delete();
                    return;
                }

                effectComponent.Amount = Amount - 1;
                Inception.Add(effectComponent);

                spawned.Add(newEffect);
            }

            SpreadToDir(Direction.North);
            SpreadToDir(Direction.East);
            SpreadToDir(Direction.South);
            SpreadToDir(Direction.West);

            Owner.SendMessage(this, new AreaEffectSpreadMessage(spawned));
        }

        public void Kill()
        {
            Inception?.Remove(this);
            Owner.SendMessage(this, new AreaEffectKillMessage());
        }

        public void React(float averageExpositions, IMapManager mapManager, IPrototypeManager prototypeManager)
        {
            Owner.SendMessage(this, new AreaEffectReactMessage(averageExpositions, mapManager, prototypeManager));
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Inception?.Remove(this);
        }

    }

    public class AreaEffectInception
    {
        private const float ReactionDelay = 0.5f;

        private readonly HashSet<AreaEffectComponent> _group = new();

        private float _lifeTimer;
        private float _spreadTimer;
        private float _reactionTimer;

        private int _amountCounterSpreading;
        private int _amountCounterRemoving;

        private readonly float _duration;
        private readonly float _spreadDelay;
        private readonly float _removeDelay;
        private readonly float _averageExpositions;

        public AreaEffectInception(int amount, float duration, float spreadDelay, float removeDelay)
        {
            _amountCounterSpreading = amount;
            _duration = duration;
            _spreadDelay = spreadDelay;
            _removeDelay = removeDelay;

            // So the first square reacts immediately after spawning
            _reactionTimer = ReactionDelay;
            /*
            The group takes amount*spreadDelay seconds to fully spread, same with fully disappearing.
            The outer squares will last duration seconds.
            The first square will last duration + how many seconds the group takes to fully spread and fully disappear, so
            it will last duration + amount*(spreadDelay+removeDelay).
            Thus, the average lifetime of the smokes will be (outerSmokeLifetime + firstSmokeLifetime)/2 = duration + amount*(spreadDelay+removeDelay)/2
            */
            _averageExpositions = (duration + amount * (spreadDelay+removeDelay) / 2)/ReactionDelay;
        }

        public bool InceptionUpdate(float frameTime, IMapManager mapManager, IPrototypeManager prototypeManager)
        {
            // Job done, return true so the system stops updating it
            if (_group.Count == 0)
                return true;

            // Make every outer square from the group spread
            if (_amountCounterSpreading != 0)
            {
                _spreadTimer += frameTime;
                if (_spreadTimer > _spreadDelay)
                {
                    _spreadTimer -= _spreadDelay;

                    var outerEffects = new HashSet<AreaEffectComponent>(_group.Where(effect => effect.Amount == _amountCounterSpreading));
                    foreach (var effect in outerEffects)
                    {
                        effect.Spread();
                    }

                    _amountCounterSpreading -= 1;
                }
            }
            // Start counting for _duration after fully spreading
            else
            {
                _lifeTimer += frameTime;
            }

            // Delete every outer square
            if (_lifeTimer > _duration)
            {
                _spreadTimer += frameTime;
                if (_spreadTimer > _removeDelay)
                {
                    _spreadTimer -= _removeDelay;

                    var outerEffects = new HashSet<AreaEffectComponent>(_group.Where(effect => effect.Amount == _amountCounterRemoving));
                    foreach (var effect in outerEffects)
                    {
                        effect.Kill();
                    }

                    _amountCounterRemoving += 1;
                }
            }

            // Make every square from the group react with the tile and entities
            _reactionTimer += frameTime;
            if (_reactionTimer > ReactionDelay)
            {
                _reactionTimer -= ReactionDelay;
                foreach (var effect in _group)
                {
                    effect.React(_averageExpositions, mapManager, prototypeManager);
                }
            }

            return false;
        }

        public void Add(AreaEffectComponent effect)
        {
            _group.Add(effect);
            effect.Inception = this;
        }

        public void Remove(AreaEffectComponent effect)
        {
            _group.Remove(effect);
            effect.Inception = null;
        }
    }

    public sealed class AreaEffectInceptionCreatedMessage : EntitySystemMessage
    {
        public AreaEffectInception Inception { get; }

        public AreaEffectInceptionCreatedMessage(AreaEffectInception inception)
        {
            Inception = inception;
        }
    }

    public sealed class AreaEffectSpreadMessage : ComponentMessage
    {
        public HashSet<IEntity> Spawned { get; }

        public AreaEffectSpreadMessage(HashSet<IEntity> spawned)
        {
            Spawned = spawned;
        }
    }

    public sealed class AreaEffectReactMessage : ComponentMessage
    {
        public float AverageExpositions { get; }
        public IMapManager MapManager { get; }
        public IPrototypeManager PrototypeManager { get; }

        public AreaEffectReactMessage(float averageExpositions, IMapManager mapManager,
            IPrototypeManager prototypeManager)
        {
            AverageExpositions = averageExpositions;
            MapManager = mapManager;
            PrototypeManager = prototypeManager;
        }
    }

    public sealed class AreaEffectKillMessage : ComponentMessage
    {

    }

}
