using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class ProjectileSpell : ITargetPointAction
    {
        public string CastMessage { get; private set; }
        public string Projectile { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.CastMessage, "castmessage", "Instant action used.");
            serializer.DataField(this, x => x.Projectile, "spellprojectile", null);
        }

        public void DoTargetPointAction(TargetPointActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(CastMessage);
        }
    }
}
