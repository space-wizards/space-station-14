using Content.Shared.Interaction.Events;
using Content.Shared.MusicPlayer;
using Robust.Shared.GameObjects;

namespace Content.Client.MusicPlayer;

public sealed class MusicRadioSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MusicRadioComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, MusicRadioComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var player = EntitySystem.Get<MusicPlayerSystem>();
        player.OpenWindow();
        args.Handled = true;
    }
}
