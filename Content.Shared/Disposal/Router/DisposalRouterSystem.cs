using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
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
    [Dependency] private readonly SharedDisposalTubeSystem _disposalTube = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetRouterNextDirection, after: new[] { typeof(SharedDisposalTubeSystem) });

        Subs.BuiEvents<DisposalRouterComponent>(DisposalRouterUiKey.Key, subs =>
        {
            subs.Event<DisposalTaggerUiActionMessage>(OnUiAction);
        });
    }

    private void OnGetRouterNextDirection(Entity<DisposalRouterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        if (!TryComp<DisposalTubeComponent>(ent, out var disposalTube))
            return;

        var exits = _disposalTube.GetTubeConnectableDirections((ent, disposalTube));

        if (exits.Length < 3 || _disposalHolder.TagsOverlap(args.Holder, ent.Comp.Tags))
        {
            _disposalTube.SelectNextTube((ent, disposalTube), exits, ref args);
            return;
        }

        _disposalTube.SelectNextTube((ent, disposalTube), exits.Skip(1).ToArray(), ref args);
    }

    /// <summary>
    /// Handles UI messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(Entity<DisposalRouterComponent> ent, ref DisposalTaggerUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (msg.Action == DisposalTaggerUiAction.Ok && SharedDisposalHolderSystem.TagRegex.IsMatch(msg.Tags))
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
