using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server.Roles
{
    public sealed class NukeopsRole : AntagonistRole
    {
        /// <summary>
        ///     Path to antagonist alert sound.
        /// </summary>
        protected override SoundSpecifier AntagonistAlert { get; } = new SoundPathSpecifier("/Audio/Ambience/Antag/nukeops_start.ogg");

        public NukeopsRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
    }
}
