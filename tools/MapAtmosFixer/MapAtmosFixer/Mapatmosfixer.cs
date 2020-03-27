using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MapAtmosFixer
{
    internal enum Objtype
    {
        Null,
        Manifold,
        Pump,
        Pipe,
        Scrubber,
        Vent,
        Mixer,
        Filter,
        Injector,
        Temp
    }

    internal static class Mapatmosfixer
    {
        /// <summary>
        ///     Used to make displayed lines shorter.
        /// </summary>
        /// <param name="path">The long path</param>
        /// <param name="masterpath">The path to short with</param>
        /// <returns>Shorted path</returns>
        public static string ShortenPath(string path, ref string masterpath)
        {
            return path.Substring(masterpath.Length, path.Length - masterpath.Length);
        }

        /// <summary>
        ///     Returns true if 'str' starts with 'start'
        /// </summary>
        /// <param name="str">String to test</param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static bool StartsWith(string str, string start)
        {
            if (start.Length > str.Length)
                return false;

            return str.Substring(0, start.Length) == start;
        }

        /// <summary>
        ///     Method to perform a simple regex match.
        /// </summary>
        /// <param name="pattern">Regex pattern</param>
        /// <param name="txt">String to match</param>
        /// <returns>Collection of matches</returns>
        public static GroupCollection Regex(string pattern, string txt)
        {
            var rgx = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(txt);

            if (matches.Count == 0)
                return null;

            GroupCollection groups = matches[0].Groups;
            if (groups.Count <= 1)
                return null;

            return groups;
        }

        /// <summary>
        ///     Gets the type of the object in the path
        /// </summary>
        /// <param name="path">String path</param>
        /// <returns></returns>
        public static Objtype GetType(string path)
        {
            if (StartsWith(path, "/obj/machinery/atmospherics/pipe/manifold"))
                return Objtype.Manifold;

            if (StartsWith(path, "/obj/machinery/atmospherics/pipe/simple"))
                return Objtype.Pipe;

            switch (path)
            {
                case "/obj/machinery/atmospherics/binary/pump":
                    return Objtype.Pump;
                case "/obj/machinery/atmospherics/unary/vent_scrubber":
                    return Objtype.Scrubber;
                case "/obj/machinery/atmospherics/unary/vent_pump":
                    return Objtype.Vent;
                case "/obj/machinery/atmospherics/trinary/filter":
                    return Objtype.Filter;
                case "/obj/machinery/atmospherics/trinary/mixer":
                    return Objtype.Mixer;
                case "/obj/machinery/atmospherics/unary/heat_reservoir/heater":
                    return Objtype.Temp;
                case "/obj/machinery/atmospherics/unary/cold_sink/freezer":
                    return Objtype.Temp;
                case "/obj/machinery/atmospherics/unary/outlet_injector":
                    return Objtype.Injector;
                default:
                    return Objtype.Null;
            }
        }

        /// <summary>
        ///     Updates an objects path with its corresponding icon_state
        /// </summary>
        /// <param name="path">Object path to change</param>
        /// <param name="iconstate">Iconstate it has</param>
        /// <param name="objtype">Type of the object</param>
        public static void ProcessIconstate(ref string path, string iconstate, Objtype objtype)
        {
            switch (objtype)
            {
                case Objtype.Pipe:
                    switch (iconstate)
                    {
                        case "intact":
                            path = "/obj/machinery/atmospherics/pipe/simple/general/visible";
                            return;
                        case "intact-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/general/hidden";
                            return;
                        case "intact-b":
                            path = "/obj/machinery/atmospherics/pipe/simple/supply/visible";
                            return;
                        case "intact-b-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/supply/hidden";
                            return;
                        case "intact-r":
                            path = "/obj/machinery/atmospherics/pipe/simple/scrubbers/visible";
                            return;
                        case "intact-r-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/scrubbers/hidden";
                            return;
                        case "intact-y":
                            path = "/obj/machinery/atmospherics/pipe/simple/yellow/visible";
                            return;
                        case "intact-y-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/yellow/hidden";
                            return;
                        case "intact-g":
                            path = "/obj/machinery/atmospherics/pipe/simple/green/visible";
                            return;
                        case "intact-g-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/green/hidden";
                            return;
                        case "intact-c":
                            path = "/obj/machinery/atmospherics/pipe/simple/cyan/visible";
                            return;
                        case "intact-c-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/cyan/hidden";
                            return;
                        case "intact-p":
                            path = "/obj/machinery/atmospherics/pipe/simple/supplymain/visible";
                            return;
                        case "intact-p-f":
                            path = "/obj/machinery/atmospherics/pipe/simple/supplymain/hidden";
                            return;
                    }
                    return;
                case Objtype.Manifold:
                    switch (iconstate)
                    {
                        case "manifold":
                            path = "/obj/machinery/atmospherics/pipe/manifold/general/visible";
                            return;
                        case "manifold-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/general/hidden";
                            return;
                        case "manifold-b":
                            path = "/obj/machinery/atmospherics/pipe/manifold/supply/visible";
                            return;
                        case "manifold-b-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/supply/hidden";
                            return;
                        case "manifold-r":
                            path = "/obj/machinery/atmospherics/pipe/manifold/scrubbers/visible";
                            return;
                        case "manifold-r-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/scrubbers/hidden";
                            return;
                        case "manifold-c":
                            path = "/obj/machinery/atmospherics/pipe/manifold/cyan/visible";
                            return;
                        case "manifold-c-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/cyan/hidden";
                            return;
                        case "manifold-y":
                            path = "/obj/machinery/atmospherics/pipe/manifold/yellow/visible";
                            return;
                        case "manifold-y-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/yellow/hidden";
                            return;
                        case "manifold-g":
                            path = "/obj/machinery/atmospherics/pipe/manifold/green/visible";
                            return;
                        case "manifold-g-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/green/hidden";
                            return;
                        case "manifold-p":
                            path = "/obj/machinery/atmospherics/pipe/manifold/supplymain/visible";
                            return;
                        case "manifold-p-f":
                            path = "/obj/machinery/atmospherics/pipe/manifold/supplymain/hidden";
                            return;
                    }
                    return;
            }
        }

        /// <summary>
        ///     Processes one object and its parameters
        /// </summary>
        /// <param name="line"></param>
        public static void ProcessObject(ref string line)
        {
            GroupCollection g = Regex(@"^(.+)\{(.+)\}", line);
            if (g == null)
                return;

            string path = g[1].Value;
            string stringtags = g[2].Value;

            Objtype objtype = GetType(path);
            if (objtype == Objtype.Null)
                return;

            var tags = new List<string>(stringtags.Split(new[] {"; "}, StringSplitOptions.None));
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];

                GroupCollection g2 = Regex(@"^(.+)[ ]=[ ](.+)", tag);
                if (g2 == null)
                    continue;

                string name = g2[1].Value;
                string value = g2[2].Value.Trim(new[] {'"'});

                //Removes icon_state from heaters/freezers
                if (objtype == Objtype.Temp)
                {
                    if (name == "icon_state")
                    {
                        tags.RemoveAt(i);
                        i--;
                    }
                    continue;
                }

                //General removal of tags we shouldn't have
                if (name == "pipe_color" || name == "color" || name == "level" ||
                    (objtype != Objtype.Pump && name == "name")
                    || (objtype == Objtype.Pump && name == "icon_state"))
                {
                    tags.RemoveAt(i);
                    i--;
                    continue;
                }

                //Processes icon_state into correct path
                if (name == "icon_state")
                {
                    ProcessIconstate(ref path, value, objtype);
                    tags.RemoveAt(i);
                    i--;
                    continue;
                }

                //Fixes up injector
                if (objtype == Objtype.Injector && name == "on")
                {
                    path = "/obj/machinery/atmospherics/unary/outlet_injector/on";
                    tags.RemoveAt(i);
                    i--;
                }
            }

            stringtags = String.Join("; ", tags);
            line = String.Format("{0}{{{1}}}", path, stringtags);
        }

        /// <summary>
        ///     This fixes connectors to their proper path, if they ain't on plating, they should be visible.
        /// </summary>
        /// <param name="line"></param>
        public static void FixConnector(ref string line)
        {
            //Dirty shit, don't read this
            if (line.Contains("/obj/machinery/atmospherics/portables_connector") &&
                //!line.Contains("/turf/open/floor/plating") &&                        // Most of the time connectors on plating want to be visible..
                !line.Contains("/obj/machinery/atmospherics/portables_connector/visible"))  // Makes sure we don't update same line twice
            {
                line = line.Replace("/obj/machinery/atmospherics/portables_connector",
                    "/obj/machinery/atmospherics/portables_connector/visible");
            }
        }

        /// <summary>
        ///     Processes a line of objects
        /// </summary>
        /// <param name="line"></param>
        public static void ProcessObjectline(ref string line)
        {
            FixConnector(ref line);

            string[] objs = line.Split(',');

            for (int i = 0; i < objs.Length; i++)
            {
                try
                {
                    ProcessObject(ref objs[i]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            line = String.Join(",", objs);
        }

        /// <summary>
        ///     Processes a whole .dmm
        /// </summary>
        /// <param name="file"></param>
        public static void Process(string file)
        {
            string[] lines = File.ReadAllLines(file, Encoding.Default);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Length == 0)
                    continue;

                if (line[0] != '"')
                    continue;

                GroupCollection g = Regex(@"^""([\w]+)""[ ]\=[ ]\((.+)\)", line);
                if (g == null)
                    continue;

                string letters = g[1].Value;
                string types = g[2].Value;
                ProcessObjectline(ref types);

                line = String.Format("\"{0}\" = ({1})", letters, types);

                lines[i] = line;
            }

            File.WriteAllLines(file, lines, Encoding.Default);
        }

        internal static void Init(string file)
        {
            //string exepath = "C:\\Users\\Daniel\\Documents\\GitHub\\-tg-station";
            //string file = "C:\\Users\\Daniel\\Documents\\GitHub\\-tg-station\\_maps\\map_files\\tgstation.2.1.3.dmm";

            Process(file);
            Console.WriteLine("Done");
            Console.Read();
        }
    }
}