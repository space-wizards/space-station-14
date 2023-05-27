using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server.Roles
{
    public sealed class ZombieRole : AntagonistRole
    {
        /// <summary>
        ///     Path to antagonist alert sound.
        /// </summary>
        protected override string AntagonistAlert => "/Audio/Ambience/Antag/zombie_start.ogg";

        public ZombieRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
    }
}
