using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    public sealed class CuffableSystem : SharedCuffableSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CuffableComponent, ComponentGetState>(OnCuffableGetState);
        }

        private void OnCuffableGetState(EntityUid uid, CuffableComponent component, ref ComponentGetState args)
        {
            // there are 2 approaches i can think of to handle the handcuff overlay on players
            // 1 - make the current RSI the handcuff type that's currently active. all handcuffs on the player will appear the same.
            // 2 - allow for several different player overlays for each different cuff type.
            // approach #2 would be more difficult/time consuming to do and the payoff doesn't make it worth it.
            // right now we're doing approach #1.
            HandcuffComponent? cuffs = null;
            if (component.CuffedHandCount > 0)
                TryComp(component.LastAddedCuffs, out cuffs);
            args.State = new CuffableComponentState(component.CuffedHandCount,
                component.CanStillInteract,
                cuffs?.CuffedRSI,
                $"{cuffs?.BodyIconState}-{component.CuffedHandCount}",
                cuffs?.Color);
            // the iconstate is formatted as blah-2, blah-4, blah-6, etc.
            // the number corresponds to how many hands are cuffed.
        }
    }
}
