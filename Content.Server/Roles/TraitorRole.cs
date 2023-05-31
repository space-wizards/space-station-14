using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server.Roles
{
    public sealed class TraitorRole : AntagonistRole
    {
        /// <summary>
        ///     Path to antagonist alert sound.
        /// </summary>
        protected override SoundSpecifier AntagonistAlert { get; } = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");

        public TraitorRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
    }
}
