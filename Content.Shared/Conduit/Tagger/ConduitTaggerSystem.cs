using Content.Shared.Conduit.Holder;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Conduit.Tagger;

/// <summary>
/// Handles the tagging of entities moving through conduit systems.
/// </summary>
public sealed partial class ConduitTaggerSystem : EntitySystem
{
    [Dependency] private readonly SharedConduitHolderSystem _disposalHolder = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConduitTaggerComponent, GetConduitNextDirectionEvent>(OnGetTaggerNextDirection, after: new[] { typeof(SharedConduitSystem) });
        SubscribeLocalEvent<ConduitTaggerComponent, ConduitTaggerUiActionMessage>(OnUiAction);
    }

    private void OnGetTaggerNextDirection(Entity<ConduitTaggerComponent> ent, ref GetConduitNextDirectionEvent args)
    {
        _disposalHolder.AddTag(args.Holder, ent.Comp.Tag);
    }

    private void OnUiAction(Entity<ConduitTaggerComponent> ent, ref ConduitTaggerUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (msg.Action == ConduitTaggerUiAction.Ok && SharedConduitHolderSystem.TagRegex.IsMatch(msg.Tags))
        {
            ent.Comp.Tag = msg.Tags.Trim();
            Dirty(ent);

            _audio.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }
}
