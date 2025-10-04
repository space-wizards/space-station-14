using Content.Shared.Conduit.Holder;
using Content.Shared.Conduit.Tagger;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Linq;

namespace Content.Shared.Conduit.Router;

/// <summary>
/// This system handles the routing of entities through conduit systems
/// based on what tags they possess.
/// </summary>
public sealed partial class ConduitRouterSystem : EntitySystem
{
    [Dependency] private readonly SharedConduitHolderSystem _conduitHolder = default!;
    [Dependency] private readonly SharedConduitSystem _conduit = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConduitRouterComponent, GetConduitNextDirectionEvent>(OnGetRouterNextDirection, after: new[] { typeof(SharedConduitSystem) });
        SubscribeLocalEvent<ConduitRouterComponent, ConduitTaggerUiActionMessage>(OnUiAction);
    }

    private void OnGetRouterNextDirection(Entity<ConduitRouterComponent> ent, ref GetConduitNextDirectionEvent args)
    {
        if (!TryComp<ConduitComponent>(ent, out var conduit))
            return;

        var exits = _conduit.GetConnectableDirections((ent, conduit));

        if (exits.Length < 3 || _conduitHolder.TagsOverlap(args.Holder, ent.Comp.Tags))
        {
            _conduit.SelectNextExit((ent, conduit), exits, ref args);
            return;
        }

        _conduit.SelectNextExit((ent, conduit), exits.Skip(1).ToArray(), ref args);
    }

    private void OnUiAction(Entity<ConduitRouterComponent> ent, ref ConduitTaggerUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (msg.Action == ConduitTaggerUiAction.Ok && SharedConduitHolderSystem.TagRegex.IsMatch(msg.Tags))
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
