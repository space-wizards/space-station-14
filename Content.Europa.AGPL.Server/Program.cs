using Robust.Server;

namespace Content.Europa.AGPL.Server
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ContentStart.Start(args);
        }
    }
}
