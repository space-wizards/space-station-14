#if DEBUG
#nullable enable
using System;
using System.IO;
using System.Linq;
using Content.Client.Development;
using JetBrains.Annotations;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Commands
{

    [UsedImplicitly]
    internal class GuiShaderDevCommand : IConsoleCommand
    {

        public string Command => "gui_shader_dev";

        public string Help => "Usage: gui_shader_dev <name>\n" + Description;

        public string Description => "Loads a shader onto a colored rectangle in a UI window.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var uiMan = IoCManager.Resolve<IUserInterfaceManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            switch (args.Length)
            {
                case 0:
                {
                    console.AddLine("Missing arguments.");
                    return false;
                }
                default:
                {
                    console.AddLine("Invalid arguments.");
                    return false;
                }
                case 1:
                {
                    if (!prototypeManager.HasIndex<ShaderPrototype>(args[0])) {
                        console.AddLine("Shader '" + args[0] + "' cannot be found in shader prototypes!");
                        return false;
                    }
                    var wnd = new SimpleShaderDevWindow(args[0]);
                    wnd.OpenCentered();
                    break;
                }
                case 2:
                {

                    if (!prototypeManager.HasIndex<ShaderPrototype>(args[0]))
                    {
                        console.AddLine("Shader '" + args[0] + "' cannot be found in shader prototypes!");
                        return false;
                    }

                    if (args[1] == "textured")
                    {
                        var wnd = new TexturedShaderDevWindow(args[0]);
                        wnd.OpenCentered();
                        return false;
                    }

                    if (args[1] == "simple")
                    {
                        goto case 1;
                    }

                    goto case default;
                }
                /*

                    if (!Color.TryFromName(args[1], out var color))
                    {
                        var maybeColor = Color.TryFromHex(args[1]);
                        if (maybeColor != null)
                        {
                            color = maybeColor.Value;
                        }
                    }

                    var wnd = new ShaderDevWindow(args[0], color);

                    if (args.Length > 2)
                    {
                        try
                        {
                            wnd.ParseParameters(string.Join(" ", args.Skip(2)));
                        }
                        catch (Exception ex)
                        {
                            wnd.Dispose();
                            console.AddLine(ex.ToString());
                        }
                    }

                    wnd.OpenCentered();

                    break;
                 */
            }

            return false;
        }

    }

}
#endif
