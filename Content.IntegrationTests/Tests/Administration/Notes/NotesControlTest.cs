using System.Linq;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.Notes;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Database;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Administration.Notes;

/// <summary>
/// Test that the admin notes UI can be used to add a new note.
/// </summary>
public sealed class NotesControlTest : InteractionTest
{
    protected override PoolSettings Settings => new() {Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false};

    [Test]
    public async Task TestNotesControl()
    {
        // Click the ahelp button in the menu bar
        await ClickWidgetControl<GameTopMenuBar, MenuButton>(nameof(GameTopMenuBar.AHelpButton));
        var bwoink = GetWindow<BwoinkWindow>();

        // Damn, if only I had an excuse to use bwoink.Bwoink.BwoinkArea
        var players = bwoink.Bwoink.ChannelSelector.PlayerListContainer;

        // Check that the player is in the menu, and make sure it is selected
        var entry = players.Data.Cast<PlayerListData>().Single(x => x.Info.SessionId == ServerSession.UserId);
        await Client.WaitPost(() => players.Select(entry));

        // Open their notes
        await ClickControl(bwoink.Bwoink.Notes);
        var noteCtrl = GetWindow<AdminNotesWindow>().Notes;
        Assert.That(noteCtrl.Notes.ChildCount, Is.EqualTo(0));

        // Add a new note
        await ClickControl(noteCtrl.NewNoteButton);
        var addNoteWindow = GetWindow<NoteEdit>();
        var msg = $"note: {Guid.NewGuid()}";
        await Client.WaitPost(() => addNoteWindow.NoteTextEdit.TextRope = new Rope.Leaf(msg));
        addNoteWindow.NoteSeverity = NoteSeverity.None;

        // Have to click submit twice for confirmation?
        await ClickControl(addNoteWindow.SubmitButton);
        await ClickControl(addNoteWindow.SubmitButton);

        // Check that the new note exists
        await RunTicks(5);
        Assert.That(noteCtrl.Notes.ChildCount, Is.EqualTo(1));
        var note = (AdminNotesLine)noteCtrl.Notes.Children[0];
        Assert.That(note.Note.Message, Is.EqualTo(msg));
    }
}
