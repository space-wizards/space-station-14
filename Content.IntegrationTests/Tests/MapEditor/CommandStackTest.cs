using System;
using Content.MapEditor.Commands;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests.MapEditor;

[TestFixture]
public sealed class CommandStackTest
{
    [Test]
    public void ExecuteAndUndo_RoundTrips()
    {
        var stack = new CommandStack();
        var value = 0;
        var cmd = new TestCommand(() => value = 1, () => value = 0);

        stack.Execute(cmd);
        Assert.That(value, Is.EqualTo(1));

        stack.Undo();
        Assert.That(value, Is.EqualTo(0));

        stack.Redo();
        Assert.That(value, Is.EqualTo(1));
    }

    [Test]
    public void NewCommand_ClearsRedoStack()
    {
        var stack = new CommandStack();
        var value = 0;
        stack.Execute(new TestCommand(() => value++, () => value--));
        stack.Execute(new TestCommand(() => value += 10, () => value -= 10));

        stack.Undo(); // undo the +10
        Assert.That(value, Is.EqualTo(1));
        Assert.That(stack.CanRedo, Is.True);

        stack.Execute(new TestCommand(() => value += 100, () => value -= 100));
        Assert.That(stack.CanRedo, Is.False); // redo cleared
    }

    [Test]
    public void Undo_WhenEmpty_DoesNothing()
    {
        var stack = new CommandStack();
        Assert.That(stack.CanUndo, Is.False);
        stack.Undo(); // should not throw
    }

    [Test]
    public void Redo_WhenEmpty_DoesNothing()
    {
        var stack = new CommandStack();
        Assert.That(stack.CanRedo, Is.False);
        stack.Redo(); // should not throw
    }

    [Test]
    public void Push_AddsWithoutExecuting()
    {
        var stack = new CommandStack();
        var executed = false;
        var cmd = new TestCommand(() => executed = true, () => executed = false);

        stack.Push(cmd);
        Assert.That(executed, Is.False); // Push should NOT call Execute
        Assert.That(stack.CanUndo, Is.True);

        stack.Undo();
        Assert.That(executed, Is.False); // Undo calls Undo(), setting to false (already false)
    }

    [Test]
    public void Clear_EmptiesBothStacks()
    {
        var stack = new CommandStack();
        var value = 0;
        stack.Execute(new TestCommand(() => value++, () => value--));
        stack.Execute(new TestCommand(() => value++, () => value--));
        stack.Undo();

        Assert.That(stack.CanUndo, Is.True);
        Assert.That(stack.CanRedo, Is.True);

        stack.Clear();
        Assert.That(stack.CanUndo, Is.False);
        Assert.That(stack.CanRedo, Is.False);
    }

    private sealed class TestCommand : IEditorCommand
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public TestCommand(Action execute, Action undo)
        {
            _execute = execute;
            _undo = undo;
        }

        public void Execute() => _execute();
        public void Undo() => _undo();
    }
}
