using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects
{
    public sealed class PopupMessage : ReagentEffect
    {
        [DataField("messages", required: true)]
        public string[] Messages = default!;

        [DataField("type")]
        public PopupType Type = PopupType.Local;

        public override void Effect(ReagentEffectArgs args)
        {
            var popupSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var msg = random.Pick(Messages);
            if (Type == PopupType.Local)
                popupSys.PopupEntity(Loc.GetString(msg), args.SolutionEntity, Filter.Entities(args.SolutionEntity));
            else if (Type == PopupType.Pvs)
                popupSys.PopupEntity(Loc.GetString(msg), args.SolutionEntity, Filter.Pvs(args.SolutionEntity));
        }
    }

    public enum PopupType
    {
        Pvs,
        Local
    }
}
