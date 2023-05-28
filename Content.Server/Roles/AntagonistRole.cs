using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Roles
{
    public class AntagonistRole : Role
    {
        /// <summary>
        ///     Path to antagonist alert sound.
        ///     TODO: Traitor sound will be a default one, cause there is no other sounds right now.
        /// </summary>
        protected virtual string AntagonistAlert => "/Audio/Ambience/Antag/traitor_start.ogg";

        public AntagPrototype Prototype { get; }

        public override string Name { get; }

        public override bool Antagonist { get; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="mind">A mind (player)</param>
        /// <param name="antagPrototype">Antagonist prototype</param>
        public AntagonistRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = Loc.GetString(antagPrototype.Name);
            Antagonist = antagPrototype.Antagonist;
        }

        public override void Greet()
        {
            base.Greet();

            // Alert a player about antagonist role with a sound notification
            var entMgr = IoCManager.Resolve<IEntityManager>();
            entMgr.EntitySysManager.TryGetEntitySystem(out SharedAudioSystem? audio);
            if (audio != null && Mind.Session != null)
            {
                audio.PlayGlobal(
                    AntagonistAlert,
                    Filter.Empty().AddPlayer(Mind.Session),
                    false,
                    AudioParams.Default);
            }
        }
    }
}
