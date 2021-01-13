#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Used to clone its owner repeatedly and group up them all so they behave like one unit, that way you can have
    /// effects that cover an area. Used by <see cref="SmokeComponent"/> and <see cref="FoamComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class AreaEffectComponent : Component
    {
        public override string Name => "AreaEffect";

        [ComponentDependency] private readonly SnapGridComponent? _snapGridComponent = default!;
        public int Amount { get; set; }
        public AreaEffectInceptionComponent? Inception { get; set; }

        /// <summary>
        /// Adds an AreaEffectInceptionComponent to this entity so the effect starts spreading and reacting.
        /// </summary>
        /// <param name="amount">The range of the effect</param>
        /// <param name="duration"></param>
        /// <param name="spreadDelay"></param>
        /// <param name="removeDelay"></param>
        public void Start(int amount, float duration, float spreadDelay, float removeDelay)
        {
            if (Inception != null)
                return;

            Amount = amount;
            var inception = Owner.EnsureComponent<AreaEffectInceptionComponent>();

            inception.Add(this);
            inception.Setup(amount, duration, spreadDelay, removeDelay);
        }

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent. "Clones" Owner in the four directions and sends a spread message
        /// so other specific components can do things to the newly spawned entities.
        /// </summary>
        public void Spread()
        {
            if (Owner.Prototype == null)
            {
                Logger.Error("AreaEffectComponent needs its owner to be spawned by a prototype.");
                return;
            }

            if (_snapGridComponent == null)
            {
                Logger.Error("AreaEffectComponent attached to "+Owner.Prototype.ID+" couldn't get SnapGridComponent from owner.");
                return;
            }

            var spawned = new HashSet<IEntity>();
            void SpreadToDir(Direction dir)
            {
                foreach (var neighbor in _snapGridComponent.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out AreaEffectComponent? comp) && comp.Inception == Inception)
                        return;

                    if (neighbor.TryGetComponent(out AirtightComponent? airtight) && airtight.AirBlocked)
                        return;
                }
                var newEffect = Owner.EntityManager.SpawnEntity(Owner.Prototype.ID, _snapGridComponent.DirectionToGrid(dir));
                var effectComponent = newEffect.EnsureComponentWarn<AreaEffectComponent>();

                effectComponent.Amount = Amount - 1;
                Inception?.Add(effectComponent);

                spawned.Add(newEffect);
            }

            SpreadToDir(Direction.North);
            SpreadToDir(Direction.East);
            SpreadToDir(Direction.South);
            SpreadToDir(Direction.West);

            SendMessage(new AreaEffectSpreadMessage(spawned));
        }

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent.
        /// Removes this component from its inception and sends a kill message. The component that receives that message
        /// should eventually delete the entity.
        /// </summary>
        public void Kill()
        {
            Inception?.Remove(this);
            SendMessage(new AreaEffectKillMessage());
        }

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent.
        /// Sends a react message so other specific components do their specific thing.
        /// </summary>
        /// <param name="averageExposures">How many times will this get called over this area effect's duration, averaged
        /// with the other area effects from the inception</param>
        public void React(float averageExposures)
        {
            SendMessage(new AreaEffectReactMessage(averageExposures));
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Inception?.Remove(this);
        }

    }

    [RegisterComponent]
    public class AreaEffectInceptionComponent : Component
    {
        public override string Name => "AreaEffectInception";

        private const float ReactionDelay = 0.5f;

        private readonly HashSet<AreaEffectComponent> _group = new();

        private float _lifeTimer;
        private float _spreadTimer;
        private float _reactionTimer;

        private int _amountCounterSpreading;
        private int _amountCounterRemoving;

        private float _duration;
        private float _spreadDelay;
        private float _removeDelay;
        private float _averageExposures;

        public void Setup(int amount, float duration, float spreadDelay, float removeDelay)
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
            _averageExposures = (duration + amount * (spreadDelay+removeDelay) / 2)/ReactionDelay;
        }

        public void InceptionUpdate(float frameTime)
        {
            _group.RemoveWhere(effect => effect.Deleted);
            if (_group.Count == 0)
                return;

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
                    effect.React(_averageExposures);
                }
            }
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
        public float AverageExposures { get; }

        public AreaEffectReactMessage(float averageExposures)
        {
            AverageExposures = averageExposures;
        }
    }

    public sealed class AreaEffectKillMessage : ComponentMessage
    {

    }

}
