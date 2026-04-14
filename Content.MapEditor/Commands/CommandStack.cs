using System.Collections.Generic;

namespace Content.MapEditor.Commands;

/// <summary>
///     Undo/redo command stack for the map editor.
/// </summary>
public sealed class CommandStack
{
    private readonly List<IEditorCommand> _undoStack = new();
    private readonly List<IEditorCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    ///     Execute a command and push it onto the undo stack.
    /// </summary>
    public void Execute(IEditorCommand command)
    {
        command.Execute();
        _undoStack.Add(command);
        _redoStack.Clear();
    }

    /// <summary>
    ///     Push a command that has already been executed onto the undo stack.
    ///     Used by tools that apply changes incrementally during a drag stroke.
    /// </summary>
    public void Push(IEditorCommand command)
    {
        _undoStack.Add(command);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (!CanUndo)
            return;

        var cmd = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        cmd.Undo();
        _redoStack.Add(cmd);
    }

    public void Redo()
    {
        if (!CanRedo)
            return;

        var cmd = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        cmd.Execute();
        _undoStack.Add(cmd);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
