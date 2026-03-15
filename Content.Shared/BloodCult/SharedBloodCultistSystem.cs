using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Content.Shared.BloodCult.Components;
using Content.Shared.Antag;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.BloodCult;

public abstract class SharedBloodCultistSystem : EntitySystem
{
    [Dependency] private readonly BloodCultistMetabolismSystem _bloodCultistMetabolism = default!;

    public override void Initialize()
    {
        base.Initialize();

		SubscribeLocalEvent<BloodCultistComponent, ComponentGetStateAttemptEvent>(OnCultistCompGetStateAttempt);
		SubscribeLocalEvent<BloodCultistComponent, ComponentStartup>(DirtyRevComps);
	}
	
	public override void Shutdown()
	{
		base.Shutdown();
	}

	/// <summary>
    /// Determines if a BloodCultist component should be sent to the client.
    /// </summary>
    private void OnCultistCompGetStateAttempt(Entity<BloodCultistComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }
	/// <summary>
    /// The criteria that determine whether a BloodCultist component should be sent to a client.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player)
    {
        if (player?.AttachedEntity is not {} uid)
            return true;

        if (HasComp<BloodCultistComponent>(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }

    private void DirtyRevComps(Entity<BloodCultistComponent> ent, ref ComponentStartup args)
    {
        var cultComps = AllEntityQuery<BloodCultistComponent>();
        while (cultComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        _bloodCultistMetabolism.ApplyUnholyBloodIfCultistWithBloodstream(ent.Owner);
    }

}

[Serializable, NetSerializable]
public enum BloodCultistCommuneUIKey : byte
{
	Key
}

[Serializable, NetSerializable]
public sealed class BloodCultCommuneBuiState : BoundUserInterfaceState
{
	public readonly string Message;

	public BloodCultCommuneBuiState(string message)
	{
		Message = message;
	}
}

[Serializable, NetSerializable]
public sealed class BloodCultCommuneSendMessage : BoundUserInterfaceMessage
{
    public readonly string Message;

    public BloodCultCommuneSendMessage(string message)
    {
        Message = message;
    }
}
