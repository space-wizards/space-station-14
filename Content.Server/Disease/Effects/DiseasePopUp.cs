using Content.Shared.Disease;
using Content.Shared.Popups;
using Robust.Shared.Player;
using JetBrains.Annotations;
using Content.Shared.IdentityManagement;

namespace Content.Server.Disease.Effects
{
    [UsedImplicitly]
    /// <summary>
    /// Plays a popup on the host's transform.
    /// Supports passing the host's entity metadata
    /// in PVS ones with {$person}
    /// </summary>
    public sealed class DiseasePopUp : DiseaseEffect
    {
        [DataField("message")]
        public string Message = "disease-sick-generic";

        [DataField("type")]
        public PopupRecipients Type = PopupRecipients.Local;

        [DataField("visualType")]
        public PopupType VisualType = PopupType.Small;

        public override void Effect(DiseaseEffectArgs args)
        {
            var popupSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();

            if (Type == PopupRecipients.Local)
                popupSys.PopupEntity(Loc.GetString(Message), args.DiseasedEntity, args.DiseasedEntity, VisualType);
            else if (Type == PopupRecipients.Pvs)
                popupSys.PopupEntity(Loc.GetString(Message, ("person", Identity.Entity(args.DiseasedEntity, args.EntityManager))), args.DiseasedEntity, VisualType);
        }

    }

    public enum PopupRecipients
    {
        Pvs,
        Local
    }
}
