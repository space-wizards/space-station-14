using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects
{
    public sealed partial class PopupMessage : EntityEffect
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

        public override void Effect(EntityEffectBaseArgs args)
        {
            var popupSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var msg = random.Pick(Messages);
            var msgArgs = new (string, object)[]
            {
                ("entity", args.TargetEntity),
            };

            if (args is EntityEffectReagentArgs reagentArgs)
            {
                msgArgs = new (string, object)[]
                {
                    ("entity", reagentArgs.TargetEntity),
                    ("organ", reagentArgs.OrganEntity.GetValueOrDefault()),
                };
            }

            if (Type == PopupRecipients.Local)
                popupSys.PopupEntity(Loc.GetString(msg, msgArgs), args.TargetEntity, args.TargetEntity, VisualType);
            else if (Type == PopupRecipients.Pvs)
                popupSys.PopupEntity(Loc.GetString(msg, msgArgs), args.TargetEntity, VisualType);
        }
    }

    public enum PopupRecipients
    {
        Pvs,
        Local
    }
}
