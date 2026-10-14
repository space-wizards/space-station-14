using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Robust.Client.Player;

namespace Content.Client.Kitchen.EntitySystems;

/// <inheritdoc />
public sealed partial class ClientMicrowaveSystem : MicrowaveSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void UpdateUI(Entity<MicrowaveComponent?> microwave)
    {
        base.UpdateUI(microwave);

        if (!MicrowaveQuery.Resolve(microwave.Owner, ref microwave.Comp))
            return;

        if (_userInterface.TryGetOpenUi(microwave.Owner, MicrowaveUiKey.Key, out var bui))
            bui.Update();
    }

    protected override void ActivateMicrowaveCycle(Entity<MicrowaveComponent> ent)
    {
        base.ActivateMicrowaveCycle(ent);

        if (!Timing.IsFirstTimePredicted)
            return;

        var audioParams = ent.Comp.LoopingSound.Params;
        audioParams = audioParams.WithLoop(true).WithMaxDistance(5);

        ent.Comp.PlayingStream ??= Audio.PlayLocal(ent.Comp.LoopingSound, ent, _player.LocalEntity, audioParams)?.Entity;
    }

    protected override void DeactivateMicrowaveCycle(Entity<MicrowaveComponent> ent)
    {
        base.DeactivateMicrowaveCycle(ent);

        // We have this check here so Audio.Stop doesn't mistakenly tell us that the entity has been deleted when it hasn't been!!!
        if (!Timing.IsFirstTimePredicted)
            return;

        ent.Comp.PlayingStream = Audio.Stop(ent.Comp.PlayingStream);

        foreach (var solid in GetMicrowaveContents(ent.AsNullable()))
        {
            RemComp<ActivelyMicrowavedComponent>(solid);
        }
    }
}
