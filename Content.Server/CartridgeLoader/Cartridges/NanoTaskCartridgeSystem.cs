using Content.Server.Paper;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class NanoTaskCartridgeSystem : SharedNanoTaskCartridgeSystem
{
    [SubscribeLocalEvent]
    private void OnNanoTaskCopied(Entity<NanoTaskPrintedComponent> original, ref PaperCopiedEvent evt)
    {
        var newTask = EnsureComp<NanoTaskPrintedComponent>(evt.Copy);
        newTask.Task = original.Comp.Task;
    }
}

