// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.SS220.BackendApi
{
    internal sealed class ControllerConsole : IConsoleShell
    {
        private readonly ConsoleShell _defaultShell;

        public IConsoleHost ConsoleHost => throw new NotSupportedException();

        public bool IsLocal => throw new NotSupportedException();

        public bool IsServer => throw new NotSupportedException();

        public ICommonSession? Player => null;

        public string ErrorMsg { get; private set; } = string.Empty;

        public string ResultMsg { get; private set; } = string.Empty;

        public ControllerConsole(ConsoleShell defaultShell)
        {
            _defaultShell = defaultShell;
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public void ExecuteCommand(string command)
        {
            throw new NotSupportedException();
        }

        public void RemoteExecuteCommand(string command)
        {
            throw new NotSupportedException();
        }

        public void WriteError(string text)
        {
            ErrorMsg += text;

            _defaultShell.WriteError(text);
        }

        public void WriteLine(string text)
        {
            ResultMsg += text;

            _defaultShell.WriteLine(text);
        }

        public void WriteLine(FormattedMessage message)
        {
            ResultMsg += message.ToString();

            _defaultShell.WriteLine(message);
        }
    }
}
