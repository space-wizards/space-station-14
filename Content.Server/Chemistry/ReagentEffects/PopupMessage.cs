using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects
{
    public sealed class PopupMessage : ReagentEffect
    {
        [DataField("messages", required: true)]
        public string[] Messages = default!;

        [DataField("type")]
        public PopupRecipients Type = PopupRecipients.Local;

        [DataField("visualType")]
        public PopupType VisualType = PopupType.Small;

        public override void Effect(ReagentEffectArgs args)
        {
            var popupSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var msg = random.Pick(Messages);
            if (Type == PopupRecipients.Local)
                popupSys.PopupEntity(Loc.GetString(msg), args.SolutionEntity, Filter.Entities(args.SolutionEntity), VisualType);
            else if (Type == PopupRecipients.Pvs)
                popupSys.PopupEntity(Loc.GetString(msg), args.SolutionEntity, Filter.Pvs(args.SolutionEntity), VisualType);
        }
    }

    public enum PopupRecipients
    {
        Pvs,
        Local
    }
}
