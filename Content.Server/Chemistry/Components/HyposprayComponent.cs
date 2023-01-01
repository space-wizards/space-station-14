using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class HyposprayComponent : SharedHyposprayComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("clumsyFailChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ClumsyFailChance { get; set; } = 0.5f;

        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(5);

        [DataField("injectSound")]
        public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

        public override ComponentState GetComponentState()
        {
            var solutionSys = _entMan.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
            return solutionSys.TryGetSolution(Owner, SolutionName, out var solution)
                ? new HyposprayComponentState(solution.CurrentVolume, solution.MaxVolume)
                : new HyposprayComponentState(FixedPoint2.Zero, FixedPoint2.Zero);
        }
    }
}
