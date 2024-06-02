using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Foldable;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Clothing.EntitySystems
{
    public sealed class WeldingMaskSystem : EntitySystem
    {
        [Dependency] private readonly BlindfoldSystem _blindfoldSystem = default!;
        [Dependency] private readonly MaskSystem _maskSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WeldingMaskComponent, FoldedEvent>(OnFold);
            SubscribeLocalEvent<WeldingMaskComponent, GotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<WeldingMaskComponent, GotUnequippedEvent>(OnUnequip);
        }
        //there probably is a better way to do this but it's 6am and I haven't slept.
        private void OnEquip(Entity<WeldingMaskComponent> ent, ref GotEquippedEvent args)
        {
            ent.Comp.Equipee = args.Equipee;
        }
        private void OnUnequip(Entity<WeldingMaskComponent> ent, ref GotUnequippedEvent args)
        {
            ent.Comp.Equipee = EntityUid.Invalid;
        }

        private void OnFold(Entity<WeldingMaskComponent> ent, ref FoldedEvent args)
        {
            Dirty(ent.Owner, ent.Comp);
            if (TryComp<BlindfoldComponent>(ent.Owner, out var blindcomp) &&
                TryComp<MaskComponent>(ent.Owner, out var maskcomp))
            {
                if (args.IsFolded)
                {
                    _blindfoldSystem.SetOverride(ent.Owner, blindcomp, true, ent.Comp.Equipee);
                    _maskSystem.SetOverride(ent.Owner, maskcomp, true);
                }
                else
                {
                    _blindfoldSystem.SetOverride(ent.Owner, blindcomp, false, ent.Comp.Equipee);
                    _maskSystem.SetOverride(ent.Owner, maskcomp, false);
                }
            }
        }
    }
}
