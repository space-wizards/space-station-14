// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

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
	[Dependency] private readonly MobStateSystem _mobState = default!;

	public override void Initialize()
    {
        base.Initialize();

		SubscribeLocalEvent<BloodCultistComponent, ComponentGetStateAttemptEvent>(OnCultistCompGetStateAttempt);
		SubscribeLocalEvent<BloodCultistComponent, ComponentStartup>(DirtyRevComps);
		SubscribeLocalEvent<BloodCultConstructShellComponent, CanDropTargetEvent>(OnJuggernautShellCanDropTarget);
		SubscribeLocalEvent<BloodCultConstructShellComponent, GettingInteractedWithAttemptEvent>(OnJuggernautShellGettingInteractedWith);
		SubscribeLocalEvent<JuggernautComponent, CanDropTargetEvent>(OnJuggernautCanDropTarget);
		SubscribeLocalEvent<JuggernautComponent, GettingInteractedWithAttemptEvent>(OnJuggernautGettingInteractedWith);
	}
	
	private void OnJuggernautShellCanDropTarget(EntityUid uid, BloodCultConstructShellComponent component, ref CanDropTargetEvent args)
	{
		// Check if the dragged entity is a dead body with a mind
		args.CanDrop = _mobState.IsDead(args.Dragged) && 
		               CompOrNull<MindContainerComponent>(args.Dragged)?.Mind != null;
		args.Handled = true;
	}
	
	private void OnJuggernautShellGettingInteractedWith(EntityUid uid, BloodCultConstructShellComponent component, ref GettingInteractedWithAttemptEvent args)
	{
		// Allow interactions on juggernaut shells for drag-drop operations
		// This ensures shells can be targeted for dragging dead bodies onto them
		args.Cancelled = false;
	}
	
	private void OnJuggernautCanDropTarget(EntityUid uid, JuggernautComponent component, ref CanDropTargetEvent args)
	{
		// Only allow dropping dead bodies with minds into inactive juggernauts
		args.CanDrop = component.IsInactive && 
		               _mobState.IsDead(args.Dragged) && 
		               CompOrNull<MindContainerComponent>(args.Dragged)?.Mind != null;
		args.Handled = true;
	}
	
	private void OnJuggernautGettingInteractedWith(EntityUid uid, JuggernautComponent component, ref GettingInteractedWithAttemptEvent args)
	{
		// Allow interactions on inactive juggernauts even if they're critical/dead
		// This is needed for reactivating them by dragging bodies/soulstones onto them
		// We explicitly uncancel the event here to override any mobstate-based cancellations
		if (component.IsInactive)
		{
			args.Cancelled = false;
		}
	}
	
	public override void Shutdown()
	{
		base.Shutdown();
	}

	/// <summary>
    /// Determines if a BloodCultist component should be sent to the client.
    /// </summary>
    private void OnCultistCompGetStateAttempt(EntityUid uid, BloodCultistComponent comp, ref ComponentGetStateAttemptEvent args)
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

    private void DirtyRevComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var cultComps = AllEntityQuery<BloodCultistComponent>();
        while (cultComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
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

/// <summary>
///    Called when a revive rune is used on the target. Revives the target if
///	   and only if enough revive charges remain.
/// </summary>
public sealed class ReviveRuneAttemptEvent : CancellableEntityEventArgs
{
	public readonly EntityUid Target;
	public readonly EntityUid? User;
	public readonly EntityUid? Used;

	public ReviveRuneAttemptEvent(EntityUid target, EntityUid? user, EntityUid? used)
	{
		Target = target;
		User = user;
		Used = used;
	}
}

/// <summary>
///    Called when a target has been potentially revived by a rune.
///	   Turns a catatonic target into a ghost role.
/// </summary>
public sealed class GhostifyRuneEvent : CancellableEntityEventArgs
{
	public readonly EntityUid Target;
	public readonly EntityUid? User;
	public readonly EntityUid? Used;

	public GhostifyRuneEvent(EntityUid target, EntityUid? user, EntityUid? used)
	{
		Target = target;
		User = user;
		Used = used;
	}
}

/// <summary>
///    Called when a victim has been potentially sacrificed by a rune.
/// </summary>
public sealed class SacrificeRuneEvent : CancellableEntityEventArgs
{
	public readonly EntityUid Victim;
	public readonly EntityUid? User;
	public readonly EntityUid? Used;
	public readonly EntityUid[] Invokers;

	public SacrificeRuneEvent(EntityUid victim, EntityUid user, EntityUid? used, EntityUid[] invokers)
	{
		Victim = victim;
		User = user;
		Used = used;
		Invokers = invokers;
	}
}

/// <summary>
///    Called when a subject has been potentially converted by a rune.
/// </summary>
public sealed class ConvertRuneEvent : CancellableEntityEventArgs
{
	public readonly EntityUid Subject;
	public readonly EntityUid? User;
	public readonly EntityUid? Used;
	public readonly EntityUid[] Invokers;

	public ConvertRuneEvent(EntityUid subject, EntityUid user, EntityUid? used, EntityUid[] invokers)
	{
		Subject = subject;
		User = user;
		Used = used;
		Invokers = invokers;
	}
}
