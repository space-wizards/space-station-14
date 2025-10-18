using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Disposal.Tagger;

/// <summary>
/// Handles the tagging of entities moving through disposals.
/// </summary>
public sealed partial class DisposalTaggerSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposalHolderSystem _disposalHolder = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsNextDirectionEvent>(OnGetTaggerNextDirection, after: new[] { typeof(DisposalTubeSystem) });

        Subs.BuiEvents<DisposalTaggerComponent>(DisposalTaggerUiKey.Key, subs =>
        {
            subs.Event<DisposalTaggerUiActionMessage>(OnUiAction);
        });
    }

    private void OnGetTaggerNextDirection(Entity<DisposalTaggerComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        _disposalHolder.AddTag(args.Holder, ent.Comp.Tag);
    }

    /// <summary>
    /// Handles UI messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(Entity<DisposalTaggerComponent> ent, ref DisposalTaggerUiActionMessage msg)
    {
        if (!Exists(msg.Actor))
            return;

        // Check for correct message and ignore maleformed strings
        if (SharedDisposalHolderSystem.TagRegex.IsMatch(msg.Tags))
        {
            ent.Comp.Tag = msg.Tags.Trim();
            Dirty(ent);

            _audio.PlayPredicted(ent.Comp.ClickSound, ent, msg.Actor, AudioParams.Default.WithVolume(-2f));
        }
    }
}
