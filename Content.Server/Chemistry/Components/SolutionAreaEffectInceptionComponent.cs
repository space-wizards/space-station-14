using System.Linq;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// The "mastermind" of a SolutionAreaEffect group. It gets updated by the SolutionAreaEffectSystem and tells the
    /// group when to spread, react and remove itself. This makes the group act like a single unit.
    /// </summary>
    /// <remarks> It should only be manually added to an entity by the <see cref="SolutionAreaEffectComponent"/> and not with a prototype.</remarks>
    [RegisterComponent]
    public sealed class SolutionAreaEffectInceptionComponent : Component
    {
        private const float ReactionDelay = 1.5f;

        private readonly HashSet<SolutionAreaEffectComponent> _group = new();

        [ViewVariables] private float _lifeTimer;
        [ViewVariables] private float _spreadTimer;
        [ViewVariables] private float _reactionTimer;

        [ViewVariables] private int _amountCounterSpreading;
        [ViewVariables] private int _amountCounterRemoving;

        /// <summary>
        /// How much time to wait after fully spread before starting to remove itself.
        /// </summary>
        [ViewVariables] private float _duration;

        /// <summary>
        /// Time between each spread step. Decreasing this makes spreading faster.
        /// </summary>
        [ViewVariables] private float _spreadDelay;

        /// <summary>
        /// Time between each remove step. Decreasing this makes removing faster.
        /// </summary>
        [ViewVariables] private float _removeDelay;

        /// <summary>
        /// How many times will the effect react. As some entities from the group last a different amount of time than
        /// others, they will react a different amount of times, so we calculate the average to make the group behave
        /// a bit more uniformly.
        /// </summary>
        [ViewVariables] private float _averageExposures;

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
            if (_amountCounterSpreading > 0)
            {
                _spreadTimer += frameTime;
                if (_spreadTimer > _spreadDelay)
                {
                    _spreadTimer -= _spreadDelay;

                    var outerEffects = new HashSet<SolutionAreaEffectComponent>(_group.Where(effect => effect.Amount == _amountCounterSpreading));
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

                    var outerEffects = new HashSet<SolutionAreaEffectComponent>(_group.Where(effect => effect.Amount == _amountCounterRemoving));
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

        public void Add(SolutionAreaEffectComponent effect)
        {
            _group.Add(effect);
            effect.Inception = this;
        }

        public void Remove(SolutionAreaEffectComponent effect)
        {
            _group.Remove(effect);
            effect.Inception = null;
        }
    }
}
