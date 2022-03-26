using Content.Shared.Disease;
using Content.Shared.Popups;
using Robust.Shared.Player;
using JetBrains.Annotations;

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
        public PopupType Type = PopupType.Local;
        public override void Effect(DiseaseEffectArgs args)
        {
            var popupSys =  EntitySystem.Get<SharedPopupSystem>();

            if (Type == PopupType.Local)
                popupSys.PopupEntity(Loc.GetString(Message), args.DiseasedEntity, Filter.Entities(args.DiseasedEntity));
            else if (Type == PopupType.Pvs)
                popupSys.PopupEntity(Loc.GetString(Message, ("person", args.DiseasedEntity)), args.DiseasedEntity, Filter.Pvs(args.DiseasedEntity));
        }

    }

    public enum PopupType
    {
        Pvs,
        Local
    }
}
