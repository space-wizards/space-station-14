using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class PlaySoundOperator : HTNOperator
{
    private AudioSystem _audio = default!;

    [DataField(required: true)]
    public SoundSpecifier? Sound;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _audio = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var uid = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        _audio.PlayPvs(Sound, uid);

        return base.Update(blackboard, frameTime);
    }
}
