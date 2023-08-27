using Content.Client.CharacterInfo;
using Content.Client.Message;
using Content.Shared.Points;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;

namespace Content.Client.Points;

/// <inheritdoc/>
public sealed class PointSystem : SharedPointSystem
{
    [Dependency] private readonly CharacterInfoSystem _characterInfo = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PointManagerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnHandleState(EntityUid uid, PointManagerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PointManagerComponentState state)
            return;

        component.Points = new(state.Points);
        component.Scoreboard = state.Scoreboard;
        _characterInfo.RequestCharacterInfo();
    }

    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        foreach (var point in EntityQuery<PointManagerComponent>())
        {
            var box = new BoxContainer
            {
                Margin = new Thickness(5),
                Orientation = BoxContainer.LayoutOrientation.Vertical
            };

            var title = new RichTextLabel
            {
                HorizontalAlignment = Control.HAlignment.Center
            };
            title.SetMarkup(Loc.GetString("point-scoreboard-header"));

            var text = new RichTextLabel();
            text.SetMessage(point.Scoreboard);

            box.AddChild(title);
            box.AddChild(text);
            ev.Controls.Add(box);
        }
    }
}
