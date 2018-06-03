using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Heed_Etyliv
{
    class Program
    {
        public static FileSystemWatcher fileSystemWatcher;
        public static List<Portrait> portraits;
        public static int[] tracker;
        public static DateTime t;
        public static DateTime f;

        static void Main(string[] args)
        {
            fileSystemWatcher = new FileSystemWatcher(); 
            fileSystemWatcher.Changed += FileSystemWatcher_Changed; 
            fileSystemWatcher.Path = AppDomain.CurrentDomain.BaseDirectory; 
            fileSystemWatcher.EnableRaisingEvents = true;

            // initialise portraits list
            portraits = new List<Portrait>();
            foreach (Id i in Enum.GetValues(typeof(Id)))
                portraits.Add(new Portrait(i));

            // initialise tracker array
            tracker = new int[6];
            
            // print rules
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" RULES:");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" - the save file may only be renamed and");
            Console.WriteLine("   edited for window size and position");
            Console.WriteLine(" - all other initial save data");
            Console.WriteLine("   and options must be vanilla");
            Console.Write(" - time starts when the ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Prisoner");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" is unlocked");
            Console.Write(" - time ends when the ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("King");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" has been played");
            Console.WriteLine();
            Console.WriteLine(" this tracker is available at:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" " + "https://github.com/YinYin-Falcon/Heed-Etyliv");

            Console.ReadLine();
        }
 
        private static void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.Clear();
            UpdateStatus(Path.Combine(fileSystemWatcher.Path, e.Name));
            PrintStatus();
        }

        private static string Read(string FileName)
        {
            // try opening the file, might still be accessed by the game
            FileStream finp;
            try { finp = new FileStream(FileName, FileMode.Open); }
            catch (Exception e) { return null; }

            // read the file
            StringBuilder sb = new StringBuilder();
            int b;
            while ((b = finp.ReadByte()) != -1)
                sb.Append((char)b);
            finp.Close();
            
            // split out the save data sub string
            String st = sb.ToString();
            int from = st.IndexOf("--------------POLICE LINE-----------------------------DO NOT CROSS--------------\n") + "--------------POLICE LINE-----------------------------DO NOT CROSS--------------\n".Length;
            int to = st.LastIndexOf("--------------DO NOT CROSS-----------------------------POLICE LINE--------------\n");
            if (to - from < 0)
                return null;
            st = st.Substring(from, to - from);

            // convert it
            string result = "";
            st = st.Replace("\n", "");
            string[] split = st.Split('O');
            for (int i = 0; i < split.Length; i++)
            {
                int c;
                if (Int32.TryParse(split[i], out c))
                    result += (char)c;
            }

            return result;
        }

        public static bool IsFileReady(string filename)
        {
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void UpdateStatus(string FileName)
        {
            // read file, abort on failure
            String read = Read(FileName);
            if (read == null)
                return;

            // split out portrait progress
            int from = read.IndexOf("16: [") + "16: [".Length;
            int to = read.IndexOf("],\n", from);
            if (to - from < 0)
                return;
            string l16 = read.Substring(from, to - from);
            l16 = Regex.Replace(l16, @"[ ]|[\[]|[\]]", "");
            string[] split = l16.Split(',');
            if (split.Length != 60)
                return;
            Portrait currentportrait = (Portrait)portraits.Find(p => p.id == Id.Prisoner);
            int r = 0;
            for (int i = 0; i < split.Length; i++)
                switch (r)
                {
                    case 0:
                        r = 1;
                        int c;
                        if (Int32.TryParse(split[i], out c))
                            currentportrait = (Portrait)portraits.Find(p => p.id == (Id)c);
                        break;
                    case 1:
                        r = 2;
                        if (currentportrait == null)
                            break;
                        if (split[i].Contains("a"))
                            currentportrait.unlocked = false;
                        else
                        {
                            currentportrait.unlocked = true;
                            if (currentportrait.name == "Prisoner" && t.Year == 1)
                                t = DateTime.Now;
                        }
                        break;
                    case 2:
                        r = 0;
                        if (currentportrait == null)
                            break;
                        if (split[i] == "0")
                            currentportrait.played = false;
                        else
                        {
                            portraits.Remove(currentportrait);
                            //currentportrait.played = true;
                            if (portraits.Count == 0 && f.Year == 1)
                                f = DateTime.Now;
                        }
                        break;
                }

            // split out individual portrait progress
            from = read.IndexOf("19: [") + "19: [".Length;
            to = read.IndexOf("],\n", from);
            if (to - from < 0)
                return;
            string l19 = read.Substring(from, to - from);
            l19 = Regex.Replace(l19, @"[ ]|[\[]|[\]]", "");
            split = l19.Split(',');
            if (split.Length == tracker.Length)
                for (int i = 0; i < split.Length; i++)
                {
                    int c;
                    if (Int32.TryParse(split[i], out c))
                        tracker[i] = c;
                }
        }

        private static void PrintStatus()
        {
            // print current or final time
            if (portraits.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(DateTime.Now - t);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(f - t);
            }

            // sort portraits
            portraits.Sort(delegate(Portrait a, Portrait b)
            {
                int xdiff = b.unlocked.CompareTo(a.unlocked);
                if (xdiff != 0) return xdiff;
                else return a.name.CompareTo(b.name);
            });

            // print progress
            for (int i = 0; i < portraits.Count; i++)
            {
                if (portraits[i].unlocked)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                if (!portraits[i].played)
                {
                    Console.Write(portraits[i].name);
                    if (!portraits[i].unlocked)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        switch (portraits[i].name)
                        {
                            case "Demon":
                                Console.Write(100 - tracker[0]);
                                break;
                            case "Dead":
                                Console.Write(100 - tracker[1]);
                                break;
                            case "Fleshman":
                                Console.Write(50 - tracker[2]);
                                break;
                            case "Absurd":
                                Console.Write(50 - tracker[4]);
                                break;
                            case "Ghost":
                                Console.Write(50 - tracker[5]);
                                break;
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        [System.Serializable]
        public class Portrait
        {
            public string name;
            public bool unlocked;
            public bool played;
            public Id id;

            public Portrait(Id i)
            {
                name = Enum.GetName(typeof(Id), i);
                unlocked = false;
                played = false;
                id = i;
            }
        }

        public enum Id
        {
            Absurd = 56,
            Ant = 43,
            Bat = 44,
            Bird = 59,
            Boy = 57,
            Bruce = 58,
            Confused = 40,
            Dead = 52,
            Demon = 51,
            Exhausted = 54,
            Fleshman = 55,
            Ghost = 47,
            King = 50,
            Monster = 45,
            Plastic = 38,
            Prisoner = 0,
            Punk = 53,
            Soldier = 42,
            Tom = 46,
            Tourist = 37,
        }
        
    }
}
