using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Containers;

public sealed class ThrowInsertContainerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowInsertContainerComponent, ThrowHitByEvent>(OnThrowCollide);
    }

    private void OnThrowCollide(Entity<ThrowInsertContainerComponent> ent, ref ThrowHitByEvent args)
    {
        var container = _containerSystem.GetContainer(ent, ent.Comp.ContainerId);

        if (!_containerSystem.CanInsert(args.Thrown, container))
            return;

        if (_random.Prob(ent.Comp.Probability))
        {
            _audio.PlayPvs(ent.Comp.MissSound, ent);
            _popup.PopupEntity(Loc.GetString(ent.Comp.MissLocString), ent);
            return;
        }

        if (!_containerSystem.Insert(args.Thrown, container))
            throw new InvalidOperationException("Container insertion failed but CanInsert returned true");

        _audio.PlayPvs(ent.Comp.InsertSound, ent);

        if (args.Component.Thrower != null)
            _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(args.Thrown)} thrown by {ToPrettyString(args.Component.Thrower.Value):player} landed in {ToPrettyString(ent)}");
    }
}
