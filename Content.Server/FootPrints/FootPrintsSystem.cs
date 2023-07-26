using Content.Shared.FootPrints;
using Robust.Shared.Timing;
using Content.Server.Decals;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Content.Server.Atmos.Components;

namespace Contest.Server.FootPrints
{
    public sealed class FootPrintsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly DecalSystem _decals = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
        }

        public void OnStartupComponent(EntityUid uid, FootPrintsComponent comp, ComponentStartup args)
        {
            if (!TryComp<TransformComponent>(uid, out var transform))
                return;
            comp.StepSize += _random.NextFloat(-0.05f, 0.05f);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in EntityManager.EntityQuery<FootPrintsComponent>())
            {
                if (!EntityManager.TryGetComponent<TransformComponent>(comp.Owner, out var transform))
                    continue;

                if (comp.PrintsColor.A <= 0f)
                    continue;
                if ((transform.LocalPosition - comp.StepPos).Length > comp.StepSize)
                {
                    comp.StepPos = transform.LocalPosition;
                    var coords = new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter);
                    var decalID = PickPrint(comp);
                    if (comp.RightStep)
                    {
                        coords = new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter + new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(comp.OffsetPrint));
                    }
                    else
                    {
                        coords = new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter + new Angle(transform.LocalRotation).RotateVec(comp.OffsetPrint));
                    }
                    comp.RightStep = !comp.RightStep;
                    _decals.TryAddDecal(decalID, coords, out var dID, comp.PrintsColor, Math.Round(transform.LocalRotation, 1) + Angle.FromDegrees(180f), 0, true);
                    comp.PrintsColor = ReduceAlpha(comp);
                }

            }
        }

        private Color ReduceAlpha(FootPrintsComponent comp)
        {
            if (comp.ColorQuantity > comp.ColorQuantityMax)
                comp.ColorQuantity = comp.ColorQuantityMax;
            var res = comp.PrintsColor;
            if (comp.ColorQuantity - comp.ColorReduceAlpha > 0f)
            {
                if (comp.ColorQuantity > 1f)
                    res = res.WithAlpha(1f);
                else
                    res = res.WithAlpha(comp.ColorQuantity);
                comp.ColorQuantity -= comp.ColorReduceAlpha;
            }
            else
            {
                res = res.WithAlpha(0f);
            }
            return res;
        }

        private string PickPrint (FootPrintsComponent comp)
        {
            string res;
            if(comp.RightStep)
            {
                res = comp.RightBarePrint;
            }
            else
            {
                res = comp.LeftBarePrint;
            }
            if(_inventorySystem.TryGetSlotEntity(comp.Owner, "shoes", out _, null, null))
            {
                res = comp.ShoesPrint;
            }
            if
            (
                _inventorySystem.TryGetSlotEntity(comp.Owner, "outerClothing", out var suit, null, null) &&
                _entManager.TryGetComponent<PressureProtectionComponent>(suit, out _)
            )
            {
                res = comp.SuitPrint;
            }
            return res;
        }
    }
}

