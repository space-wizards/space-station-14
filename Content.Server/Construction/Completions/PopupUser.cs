using Content.Server.Popups;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class PopupUser : IGraphAction
    {
        [DataField("cursor")] public bool Cursor { get; }
        [DataField("text")] public string Text { get; } = string.Empty;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (userUid == null)
                return;

            var popupSystem = entityManager.EntitySysManager.GetEntitySystem<PopupSystem>();

            if(Cursor)
                popupSystem.PopupCursor(Loc.GetString(Text), userUid.Value);
            else
                popupSystem.PopupEntity(Loc.GetString(Text), uid, userUid.Value);
        }
    }
}
