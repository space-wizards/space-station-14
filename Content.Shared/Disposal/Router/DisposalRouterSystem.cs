using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Linq;

namespace Content.Shared.Disposal.Router;

/// <summary>
/// This system handles the routing of entities in disposals
/// based on what tags they possess.
/// </summary>
public sealed partial class DisposalRouterSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposalHolderSystem _disposalHolder = default!;
    [Dependency] private readonly DisposalTubeSystem _disposalTube = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetRouterNextDirection, before: new[] { typeof(DisposalTubeSystem) });

        Subs.BuiEvents<DisposalRouterComponent>(DisposalRouterUiKey.Key, subs =>
        {
            subs.Event<DisposalRouterUiActionMessage>(OnUiAction);
        });
    }

    private void OnGetRouterNextDirection(Entity<DisposalRouterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DisposalTubeComponent>(ent, out var disposalTube))
            return;

        var exits = _disposalTube.GetTubeConnectableDirections((ent, disposalTube));

        if (exits.Length < 3 || _disposalHolder.TagsOverlap(args.Holder, ent.Comp.Tags))
        {
            _disposalTube.SelectNextDirection((ent, disposalTube), exits, ref args);
            return;
        }

        _disposalTube.SelectNextDirection((ent, disposalTube), exits.Skip(1).ToArray(), ref args);
        args.Handled = true;
    }

    private void OnUiAction(Entity<DisposalRouterComponent> ent, ref DisposalRouterUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (SharedDisposalHolderSystem.TagRegex.IsMatch(msg.Tags))
        {
            ent.Comp.Tags.Clear();

            foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = tag.Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                ent.Comp.Tags.Add(trimmed);
            }

            Dirty(ent);

            _audio.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }
}
