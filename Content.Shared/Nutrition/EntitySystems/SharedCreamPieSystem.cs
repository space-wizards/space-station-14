using Content.Shared.Nutrition.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;

namespace Content.Shared.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedCreamPieSystem : EntitySystem
    {
        [Dependency] private SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CreamPieComponent, ThrowDoHitEvent>(OnCreamPieHit);
            SubscribeLocalEvent<CreamPieComponent, LandEvent>(OnCreamPieLand);
            SubscribeLocalEvent<CreamPiedComponent, ThrowHitByEvent>(OnCreamPiedHitBy);
        }

        public void SplatCreamPie(EntityUid uid, CreamPieComponent creamPie)
        {
            // Already splatted! Do nothing.
            if (creamPie.Splatted)
                return;

            creamPie.Splatted = true;

            SplattedCreamPie(uid, creamPie);
        }

        protected virtual void SplattedCreamPie(EntityUid uid, CreamPieComponent creamPie) {}

        public void SetCreamPied(EntityUid uid, CreamPiedComponent creamPied, bool value)
        {
            if (value == creamPied.CreamPied)
                return;

            creamPied.CreamPied = value;

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                _appearance.SetData(uid, CreamPiedVisuals.Creamed, value, appearance);
            }
        }

        private void OnCreamPieLand(EntityUid uid, CreamPieComponent component, ref LandEvent args)
        {
            SplatCreamPie(uid, component);
        }

        private void OnCreamPieHit(EntityUid uid, CreamPieComponent component, ThrowDoHitEvent args)
        {
            SplatCreamPie(uid, component);
        }

        private void OnCreamPiedHitBy(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            if (!EntityManager.EntityExists(args.Thrown) || !EntityManager.TryGetComponent(args.Thrown, out CreamPieComponent? creamPie)) return;

            SetCreamPied(uid, creamPied, true);

            CreamedEntity(uid, creamPied, args);

            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(creamPie.ParalyzeTime), true);
        }

        protected virtual void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args) {}
    }
}
