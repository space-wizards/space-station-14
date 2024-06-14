using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Clothing.EntitySystems
{
    public sealed class WeldingMaskSystem : EntitySystem
    {
        [Dependency] private readonly MaskSystem _maskSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
        [Dependency] private readonly InventorySystem _invSystem = default!;
        [Dependency] private readonly FoldableSystem _foldableSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WeldingMaskComponent, FoldedEvent>(OnFold);
            SubscribeLocalEvent<WeldingMaskComponent, GotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<WeldingMaskComponent, GotUnequippedEvent>(OnUnequip);
            SubscribeLocalEvent<WeldingMaskComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<WeldingMaskComponent, ToggleWeldingEvent>(OnToggleWeld);
        }


        //there probably is a better way to do this but it's 6am and I haven't slept.
        private void OnEquip(Entity<WeldingMaskComponent> ent, ref GotEquippedEvent args)
        {
            ent.Comp.Equipee = args.Equipee;

            if (!ent.Comp.Folded)
                Enable(ent, args.Equipee);
        }
        private void OnGetActions(EntityUid uid, WeldingMaskComponent comp, GetItemActionsEvent args)
        {
            if (_invSystem.InSlotWithFlags(uid, SlotFlags.HEAD))
                args.AddAction(ref comp.ToggleActionEntity, comp.ToggleAction);
        }

        private void OnToggleWeld(Entity<WeldingMaskComponent> ent, ref ToggleWeldingEvent args)
        {
            if (TryComp<FoldableComponent>(ent.Owner, out var comp))
            {
                bool a = _foldableSystem.TryToggleFold(ent.Owner, comp);
                var ev = new ItemWeldingToggledEvent(ent.Comp.Equipee, a);
                RaiseLocalEvent(ent.Owner, ev);
            }
        }


        private void OnUnequip(Entity<WeldingMaskComponent> ent, ref GotUnequippedEvent args)
        {
            ent.Comp.Equipee = EntityUid.Invalid;
            Disable(args.Equipee);
        }

        private void OnFold(Entity<WeldingMaskComponent> ent, ref FoldedEvent args)
        {
            Dirty(ent.Owner, ent.Comp);
            if (TryComp<MaskComponent>(ent.Owner, out var maskcomp) &&
                TryComp<EyeProtectionComponent>(ent.Owner, out var eyecomp) &&
                TryComp<FlashImmunityComponent>(ent.Owner, out var flashcomp))
            {
                if (args.IsFolded)
                {
                    _maskSystem.SetOverride(ent.Owner, maskcomp, true);
                    eyecomp.ProtectionTime = TimeSpan.FromSeconds(0);
                    flashcomp.Enabled = false;
                    ent.Comp.Folded = true;
                    if (ent.Comp.Equipee != EntityUid.Invalid)
                        Disable(ent.Comp.Equipee);
                }
                else
                {
                    _maskSystem.SetOverride(ent.Owner, maskcomp, false);
                    eyecomp.ProtectionTime = TimeSpan.FromSeconds(10);
                    flashcomp.Enabled = true;
                    ent.Comp.Folded = false;
                    if (ent.Comp.Equipee != EntityUid.Invalid)
                        Enable(ent, ent.Comp.Equipee);
                }
            }
        }

        private void Enable(Entity<WeldingMaskComponent> ent, EntityUid uid)
        {
            //i have no idea why this throws a bazillion errors, but they're not actual errors
            if (EnsureComp<WeldingVisionComponent>(uid, out var comp))
            {
                comp.InnerDiameter = ent.Comp.InnerDiameter;
                comp.OuterDiameter = ent.Comp.OuterDiameter;
                Dirty(uid, comp);
            }
        }

        private void Disable(EntityUid uid)
        {
            if (TryComp<WeldingVisionComponent>(uid, out var comp))
            {
                Dirty(uid, comp);
                RemComp(uid, comp);
            }
        }
    }
}
