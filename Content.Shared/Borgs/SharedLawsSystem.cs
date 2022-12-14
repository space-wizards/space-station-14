using Robust.Shared.GameStates;

namespace Content.Shared.Borgs
{
    public sealed class SharedLawsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LawsComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<LawsComponent, ComponentHandleState>(OnHandleState);
        }
        private void OnGetState(EntityUid uid, LawsComponent component, ref ComponentGetState args)
        {
            args.State = new LawsComponentState(component.Laws);
        }

        private void OnHandleState(EntityUid uid, LawsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not LawsComponentState cast)
                return;

            component.Laws = cast.Laws;
        }
    }
}
