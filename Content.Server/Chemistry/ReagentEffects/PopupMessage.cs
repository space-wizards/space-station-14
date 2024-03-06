using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects
{
    public sealed partial class PopupMessage : ReagentEffect
    {
        [DataField(required: true)]
        public string[] Messages = default!;

        [DataField]
        public PopupRecipients Type = PopupRecipients.Local;

        [DataField]
        public PopupType VisualType = PopupType.Small;

        // JUSTIFICATION: This is purely cosmetic.
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => null;

        public override void Effect(ReagentEffectArgs args)
        {
            var popupSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var msg = random.Pick(Messages);
            var msgArgs = new (string, object)[] {
                ("entity", args.SolutionEntity),
                ("organ", args.OrganEntity.GetValueOrDefault()),
            };
            if (Type == PopupRecipients.Local)
                popupSys.PopupEntity(Loc.GetString(msg, msgArgs), args.SolutionEntity, args.SolutionEntity, VisualType);
            else if (Type == PopupRecipients.Pvs)
                popupSys.PopupEntity(Loc.GetString(msg, msgArgs), args.SolutionEntity, VisualType);
        }
    }

    public enum PopupRecipients
    {
        Pvs,
        Local
    }
}
