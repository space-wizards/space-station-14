using System.Collections.Generic;

namespace Content.MapEditor.Commands;

/// <summary>
///     Groups multiple commands into one undoable unit (e.g. a paint stroke).
/// </summary>
public sealed class BatchCommand : IEditorCommand
{
    private readonly List<IEditorCommand> _commands = new();

    public int Count => _commands.Count;

    public void Add(IEditorCommand cmd)
    {
        _commands.Add(cmd);
    }

    public void Execute()
    {
        foreach (var cmd in _commands)
            cmd.Execute();
    }

    public void Undo()
    {
        // Undo in reverse order.
        for (var i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}
