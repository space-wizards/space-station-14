using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapAtmosFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please drag-drop file onto the .exe");
                Console.Read();
                return;
            }

            string file = args[0];
            if (Path.GetExtension(file) != ".dmm")
            {
                Console.WriteLine("File not a map.");
                Console.Read();
                return;
            }

            Mapatmosfixer.Init(file);
        }
    }
}
