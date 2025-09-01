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

        public void SplatCreamPie(Entity<CreamPieComponent> creamPie)
        {
            // Already splatted! Do nothing.
            if (creamPie.Comp.Splatted)
                return;

            creamPie.Comp.Splatted = true;

            SplattedCreamPie(creamPie);
        }

        protected virtual void SplattedCreamPie(Entity<CreamPieComponent, EdibleComponent?> entity) { }

        public void SetCreamPied(EntityUid uid, CreamPiedComponent creamPied, bool value)
        {
            if (value == creamPied.CreamPied)
                return;

            creamPied.CreamPied = value;

            if (TryComp(uid, out AppearanceComponent? appearance))
            {
                _appearance.SetData(uid, CreamPiedVisuals.Creamed, value, appearance);
            }
        }

        private void OnCreamPieLand(Entity<CreamPieComponent> entity, ref LandEvent args)
        {
            SplatCreamPie(entity);
        }

        private void OnCreamPieHit(Entity<CreamPieComponent> entity, ref ThrowDoHitEvent args)
        {
            SplatCreamPie(entity);
        }

        private void OnCreamPiedHitBy(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            if (!Exists(args.Thrown) || !TryComp(args.Thrown, out CreamPieComponent? creamPie)) return;

            SetCreamPied(uid, creamPied, true);

            CreamedEntity(uid, creamPied, args);

            _stunSystem.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(creamPie.ParalyzeTime));
        }

        protected virtual void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args) {}
    }
}
