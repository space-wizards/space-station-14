namespace Content.MapEditor.Commands;

/// <summary>
///     A reversible editor command supporting undo/redo.
/// </summary>
public interface IEditorCommand
{
    void Execute();
    void Undo();
}
