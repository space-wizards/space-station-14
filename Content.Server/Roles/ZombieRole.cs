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
        protected override SoundSpecifier AntagonistAlert { get; } = new SoundPathSpecifier("/Audio/Voice/Zombie/zombie-3.ogg");

        public ZombieRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
    }
}
