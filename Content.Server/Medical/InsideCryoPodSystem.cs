using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Medical.Cryogenics;

namespace Content.Server.Medical
{
    public sealed partial class CryoPodSystem
    {
        public override void InitializeInsideCryoPod()
        {
            base.InitializeInsideCryoPod();
            // Atmos overrides
            SubscribeLocalEvent<InsideCryoPodComponent, InhaleLocationEvent>(OnInhaleLocation);
            SubscribeLocalEvent<InsideCryoPodComponent, ExhaleLocationEvent>(OnExhaleLocation);
            SubscribeLocalEvent<InsideCryoPodComponent, AtmosExposedGetAirEvent>(OnGetAir);
        }

        #region Atmos handlers

        private void OnGetAir(EntityUid uid, InsideCryoPodComponent component, ref AtmosExposedGetAirEvent args)
        {
            if (TryComp<InternalAirComponent>(Transform(uid).ParentUid, out var internalAir))
            {
                args.Gas = internalAir.Air;
                args.Handled = true;
            }
        }

        private void OnInhaleLocation(EntityUid uid, InsideCryoPodComponent component, InhaleLocationEvent args)
        {
            if (TryComp<InternalAirComponent>(Transform(uid).ParentUid, out var internalAir))
            {
                args.Gas = internalAir.Air;
            }
        }

        private void OnExhaleLocation(EntityUid uid, InsideCryoPodComponent component, ExhaleLocationEvent args)
        {
            if (TryComp<InternalAirComponent>(Transform(uid).ParentUid, out var internalAir))
            {
                args.Gas = internalAir.Air;
            }
        }

        #endregion
    }
}
