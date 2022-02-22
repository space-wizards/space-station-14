using System.Threading.Tasks;
using Content.Server.Popups;
using Content.Shared.Construction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class PopupUser : IGraphAction
    {
        [DataField("cursor")] public bool Cursor { get; } = false;
        [DataField("text")] public string Text { get; } = string.Empty;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (userUid == null)
                return;

            var popupSystem = entityManager.EntitySysManager.GetEntitySystem<PopupSystem>();

            if(Cursor)
                popupSystem.PopupCursor(Loc.GetString(Text), Filter.Entities(userUid.Value));
            else
                popupSystem.PopupEntity(Loc.GetString(Text), uid, Filter.Entities(userUid.Value));
        }
    }
}
