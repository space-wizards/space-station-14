using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Containers;

public sealed partial class ThrowInsertContainerSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowInsertContainerComponent, ThrowHitByEvent>(OnThrowCollide);
    }

    private void OnThrowCollide(Entity<ThrowInsertContainerComponent> ent, ref ThrowHitByEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var container = _containerSystem.GetContainer(ent, ent.Comp.ContainerId);

        if (!_containerSystem.CanInsert(args.Thrown, container))
            return;

        var beforeThrowArgs = new BeforeThrowInsertEvent(args.Thrown);
        RaiseLocalEvent(ent, ref beforeThrowArgs);

        if (beforeThrowArgs.Cancelled)
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.Probability, GetNetEntity(ent)))
        {
            _audio.PlayPredicted(ent.Comp.MissSound, ent, args.Thrown.Comp.Thrower);
            _popup.PopupPredicted(Loc.GetString(ent.Comp.MissLocString), ent, args.Thrown.Comp.Thrower);
            return;
        }

        if (!_containerSystem.Insert(args.Thrown.Owner, container))
            throw new InvalidOperationException("Container insertion failed but CanInsert returned true");

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, args.Thrown.Comp.Thrower);

        if (args.Thrown.Comp.Thrower != null)
            _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(args.Thrown)} thrown by {ToPrettyString(args.Thrown.Comp.Thrower.Value):player} landed in {ToPrettyString(ent)}");
    }
}
