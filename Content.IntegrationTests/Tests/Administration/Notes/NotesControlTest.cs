using Content.Client.Administration.UI.Notes;
using Content.Client.Administration.UI.PlayerPanel;
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
        await Client.ExecuteCommand($"playerpanel \"{Client.Session?.Name}\"");
        await RunTicks(5); // Wait for playerpanel to finish loading
        var playerPanelWindow = GetWindow<PlayerPanel>();

        // Open their notes
        await ClickControl(playerPanelWindow.NotesButton);
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
