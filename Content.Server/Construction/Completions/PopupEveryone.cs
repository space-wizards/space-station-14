using Content.Server.Popups;
using Content.Shared.Construction;
using Robust.Shared.Player;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public sealed partial class PopupEveryone : IGraphAction
    {
        [DataField("text")] public string Text { get; private set; } = string.Empty;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            entityManager.EntitySysManager.GetEntitySystem<PopupSystem>()
                .PopupEntity(Loc.GetString(Text), uid);
        }
    }
}
