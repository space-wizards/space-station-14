using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Server.Tools.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Tools
{
    // TODO move tool system to shared, and make it a friend of Tool Component.
    public sealed partial class ToolSystem : SharedToolSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeWelders();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateWelders(frameTime);
        }

        protected override bool IsWelder(EntityUid uid)
        {
            return HasComp<WelderComponent>(uid);
        }
    }
}
