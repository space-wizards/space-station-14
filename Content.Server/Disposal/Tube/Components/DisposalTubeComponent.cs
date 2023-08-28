using System.Linq;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
    public sealed partial class DisposalTubeComponent : Component
    {
        [DataField("containerId")] public string ContainerId { get; set; } = "DisposalTube";

        public static readonly TimeSpan ClangDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastClang;

        public bool Connected;
        [DataField("clangSound")] public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        /// <summary>
        ///     Container of entities that are currently inside this tube
        /// </summary>
        [ViewVariables]
        [Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
        public Container Contents { get; set; } = default!;
    }
}
