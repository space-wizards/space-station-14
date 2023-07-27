using Content.Shared.FootPrints;
using Robust.Shared.Timing;
using Content.Server.Decals;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Content.Server.Atmos.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Contest.Server.FootPrints
{
    public sealed class FootPrintsSystem : EntitySystem
    {
        [Dependency] private readonly DecalSystem _decals = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
        }

        public void OnStartupComponent(EntityUid uid, FootPrintsComponent comp, ComponentStartup args)
        {
            comp.StepSize += _random.NextFloat(-0.05f, 0.05f);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (!_configManager.GetCVar(CCVars.FootPrintsEnabled))
                return;
            foreach (var comp in EntityManager.EntityQuery<FootPrintsComponent>())
            {
                if (!EntityManager.TryGetComponent<TransformComponent>(comp.Owner, out var transform))
                    continue;
                if (comp.PrintsColor.A <= 0f)
                    continue;
                if (!TryComp<MobThresholdsComponent>(comp.Owner, out var mobThreshHolds))
                    continue;
                if ((transform.LocalPosition - comp.StepPos).Length > (CheckMobState(mobThreshHolds) ? comp.DragSize : comp.StepSize))
                {
                    comp.RightStep = !comp.RightStep;
                    _decals.TryAddDecal(
                        PickPrint(comp, CheckMobState(mobThreshHolds)),
                        CalcCoords(comp, transform, CheckMobState(mobThreshHolds)),
                        out var dID, comp.PrintsColor,
                        CheckMobState(mobThreshHolds) ?
                        (transform.LocalPosition - comp.StepPos).ToAngle() + Angle.FromDegrees(-90f) :
                        transform.LocalRotation + Angle.FromDegrees(180f),
                        0, true);
                    comp.PrintsColor = ReduceAlpha(comp);
                    comp.StepPos = transform.LocalPosition;
                }

            }
        }

        private EntityCoordinates CalcCoords(FootPrintsComponent comp, TransformComponent transform, bool state)
        {
            if (state)
            {
                return new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter);
            }
            else
            {
                if (comp.RightStep)
                {
                    return new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter + new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(comp.OffsetPrint));
                }
                else
                {
                    return new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter + new Angle(transform.LocalRotation).RotateVec(comp.OffsetPrint));
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

        private string PickPrint(FootPrintsComponent comp, bool state)
        {
            string res;
            if (comp.RightStep)
            {
                res = comp.RightBarePrint;
            }
            else
            {
                res = comp.LeftBarePrint;
            }
            if (_inventorySystem.TryGetSlotEntity(comp.Owner, "shoes", out _, null, null))
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
            if (state)
                res = _random.Pick(comp.DraggingPrint);
            return res;
        }

        private bool CheckMobState(MobThresholdsComponent state)
        {
            return state.CurrentThresholdState == MobState.Critical || state.CurrentThresholdState == MobState.Dead;
        }
    }
}

