using Content.Server.Administration;
using Content.Server.Machines.EntitySystems;
using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Machines.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Console;

namespace Content.Server.Singularity
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class StartSingularityEngineCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly EmitterSystem _emitterSystem = default!;
        [Dependency] private readonly MultipartMachineSystem _multipartSystem = default!;
        [Dependency] private readonly ParticleAcceleratorSystem  _paSystem = default!;
        [Dependency] private readonly RadiationCollectorSystem _radCollectorSystem = default!;

        public override string Command => "startsingularityengine";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine(Loc.GetString($"shell-need-exactly-zero-arguments"));
                return;
            }

            // Turn on emitters
            var emitterQuery = EntityManager.EntityQueryEnumerator<EmitterComponent>();
            while (emitterQuery.MoveNext(out var uid, out var emitterComponent))
            {
                //FIXME: This turns on ALL emitters, including APEs. It should only turn on the containment field emitters.
                _emitterSystem.SwitchOn(uid, emitterComponent);
            }

            // Turn on radiation collectors
            var radiationCollectorQuery = EntityManager.EntityQueryEnumerator<RadiationCollectorComponent>();
            while (radiationCollectorQuery.MoveNext(out var uid, out var radiationCollectorComponent))
            {
                _radCollectorSystem.SetCollectorEnabled(uid, enabled: true, user: null, radiationCollectorComponent);
            }

            // Setup PA
            var paQuery = EntityManager.EntityQueryEnumerator<ParticleAcceleratorControlBoxComponent>();
            while (paQuery.MoveNext(out var paId, out var paControl))
            {
                if (!EntityManager.TryGetComponent<MultipartMachineComponent>(paId, out var machine))
                    continue;

                if (!_multipartSystem.Rescan((paId, machine)))
                    continue;

                _paSystem.SetStrength(paId, ParticleAcceleratorPowerState.Level0, comp: paControl);
                _paSystem.SwitchOn(paId, comp: paControl);
            }

            shell.WriteLine(Loc.GetString($"shell-command-success"));
        }
    }
}
