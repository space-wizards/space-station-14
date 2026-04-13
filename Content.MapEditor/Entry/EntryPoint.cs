using Robust.Shared.ContentPack;

namespace Content.MapEditor.Entry;

/// <summary>
///     Content entry point for the map editor assembly.
///     Kept intentionally empty — all IoC registration and component setup is
///     handled by Content.Client's EntryPoint (which is loaded first because
///     Content.MapEditor references Content.Client). The actual state switch
///     to <see cref="MapEditorState"/> happens via the
///     <c>GameControllerOptions.PostInitCallback</c> configured in Program.cs.
/// </summary>
public sealed class EntryPoint : GameClient
{
    // No overrides needed.  Content.Client's EntryPoint handles:
    //   - ClientContentIoC.Register (PreInit)
    //   - BuildGraph, component/prototype registration (Init)
    //   - Stylesheet, input contexts, overlays (PostInit)
    //
    // Having this class exist ensures the Content.MapEditor assembly is
    // recognized as a content module by the engine's mod loader (it scans
    // for GameShared subclasses), which in turn causes the assembly's types
    // to be registered with the ReflectionManager.
}
