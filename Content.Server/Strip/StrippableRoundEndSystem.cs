using Content.Server.RoundEnd;
using Content.Shared.Strip;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Strip
{
    public sealed class StrippableRoundEndSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEnd);
        }

        private void OnRoundEnd(RoundEndSystemChangedEvent ev)
        {
            var strippableSystem = EntitySystem.Get<StrippableSystem>();
            strippableSystem.ClearActiveStripDoAfters();
        }
    }
}
