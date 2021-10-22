using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Electrocution
{
    public abstract class SharedElectrocutionSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InsulatedComponent, ElectrocutionAttemptEvent>(OnInsulatedElectrocutionAttempt);
            SubscribeLocalEvent<InsulatedComponent, ComponentGetState>(OnInsulatedGetState);
            SubscribeLocalEvent<InsulatedComponent, ComponentHandleState>(OnInsulatedHandleState);
        }

        public void SetInsulatedSiemensCoefficient(EntityUid uid, float siemensCoefficient, InsulatedComponent? insulated = null)
        {
            if (!Resolve(uid, ref insulated))
                return;

            insulated.SiemensCoefficient = siemensCoefficient;
            insulated.Dirty();
        }

        private void OnInsulatedElectrocutionAttempt(EntityUid uid, InsulatedComponent insulated, ElectrocutionAttemptEvent args)
        {
            args.SiemensCoefficient *= insulated.SiemensCoefficient;
        }

        private void OnInsulatedGetState(EntityUid uid, InsulatedComponent insulated, ref ComponentGetState args)
        {
            args.State = new InsulatedComponentState(insulated.SiemensCoefficient);
        }

        private void OnInsulatedHandleState(EntityUid uid, InsulatedComponent insulated, ref ComponentHandleState args)
        {
            if (args.Current is not InsulatedComponentState state)
                return;

            insulated.SiemensCoefficient = state.SiemensCoefficient;
        }

    }
}
