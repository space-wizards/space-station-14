using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Bots
{
    public sealed class HonkBotSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var bot in EntityQuery<HonkBotComponent>())
            {
                bot.Accumulator += frameTime;
                if (bot.Accumulator < bot.HonkRollInterval)
                {
                    continue;
                }
                bot.Accumulator -= bot.HonkRollInterval;
                if (_random.Prob(0.5f))
                {
                    SoundSystem.Play(Filter.Pvs(bot.Owner), bot.HonkSound.GetSound(), bot.Owner, AudioHelpers.WithVariation(0.125f));
                }
            }
        }
    }
}
