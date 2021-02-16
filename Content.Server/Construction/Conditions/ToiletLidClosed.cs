#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Watercloset;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class ToiletLidClosed : IEdgeCondition
    {
        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ToiletComponent? toilet)) return false;
            return !toilet.LidOpen;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inExamineRange)
        {
            if (!entity.TryGetComponent(out ToiletComponent? toilet)) return false;
            if (!toilet.LidOpen) return false;

            message.AddMarkup(Loc.GetString("Use a [color=yellow]crowbar[/color] to close the lid.\n"));
            return true;
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
        }
    }
}
