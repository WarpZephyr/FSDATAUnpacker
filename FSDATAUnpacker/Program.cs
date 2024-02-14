using Handlers;
using Structures;

namespace FSDATAUnpacker
{
    internal class Program
    {
        private static readonly Dictionary<string, int> ENTRY_COUNTS = new Dictionary<string, int>
        {
            { "ER", 4096 },
            { "AC2", 4096 },
            { "AC25", 8192 },
            { "AC3", 8192 }
        };

        static void Main(string[] args)
        {
            int argCount = args.Length;
            if (argCount < 1)
            {
                PrintHelp();
                return;
            }

            foreach (string arg in args)
            {
#if !DEBUG
                try
                {
#endif
                if (File.Exists(arg))
                {
                    Console.WriteLine($"Unpacking: {arg}");
                    UnpackFile(arg);
                }
                else if (Directory.Exists(arg))
                {
                    Console.WriteLine($"Repacking: {arg}");
                    RepackDirectory(arg);
                }
                else
                {
                    Console.WriteLine($"Warning: argument not supported: {arg}");
                }
#if !DEBUG
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error has occurred:\n{e.Message}\n{e.StackTrace}");
                }
#endif
            }

            Console.WriteLine("Finished.");
        }

        private static string GetGameType(string path)
        {
            string name = PathHandler.GetFileNameWithoutExtensions(path);

            if (name.Length < 5)
            {
                throw new NotSupportedException($"Could not get game type by name: {path}");
            }

            if (!name.EndsWith("DATA"))
            {
                throw new NotSupportedException($"Extensionless name did not end with \"DATA\" as expected: {name}");
            }

            return name[..^4];
        }

        private static int GetEntryCount(string gameType)
        {
            if (!ENTRY_COUNTS.TryGetValue(gameType, out int entryCount))
            {
                throw new NotSupportedException($"Could not get entry count for automatically discovered game type: {gameType}");
            }

            return entryCount;
        }

        private static void UnpackFile(string file)
        {
            string directory = PathHandler.GetDirectoryName(file);

            string type = GetGameType(file);
            int entryCount = GetEntryCount(type);

            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var reader = new FSDATA(entryCount, fs);

            string outputDirectory = PathHandler.Combine(directory, $"{type}DATA");
            PathExceptionHandler.ThrowIfFile(outputDirectory);
            Directory.CreateDirectory(outputDirectory);

            for (int i = 0; i < reader.Files.Count; i++)
            {
                var fileDataInfo = reader.Files[i];
                string writePath = PathHandler.Combine(outputDirectory, fileDataInfo.FileHeader.Path);
                PathExceptionHandler.ThrowIfDirectory(writePath);
                File.WriteAllBytes(writePath, fileDataInfo.DataHeader.GetBytes(fs));
            }
        }

        private static void RepackDirectory(string directory)
        {
            string parentDirectory = PathHandler.GetDirectoryName(directory);
            string name = PathHandler.GetDirectoryNameWithoutPath(directory);
           
            string type = GetGameType(name);
            int entryCount = GetEntryCount(type);

            var archive = new FSDATA(entryCount);
            var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileHeader = new FileHeader(file);
                var dataHeader = new DataHeader(-1, new FileInfo(file).Length);
                var fileDataInfo = new FileDataInfo(fileHeader, dataHeader);
                archive.Files.Add(fileDataInfo);
            }

            string writePath = PathHandler.Combine(parentDirectory, $"{type}DATA.BIN");
            if (File.Exists(writePath))
            {
                string backupPath = writePath + ".BAK";
                if (!File.Exists(backupPath))
                {
                    File.Move(writePath, backupPath);
                }
            }

            archive.Write(writePath);
        }

        private static string Supported =>
            "ERDATA.BIN:   4096\n" +
            "AC2DATA.BIN:  4096\n" +
            "AC25DATA.BIN: 8192\n" +
            "AC3DATA.BIN:  8192\n";

        private static void PrintHelp()
        {
            string guiNotice =
                "GUI Notice:\n" +
                "This program does not have a GUI.\n" +
                "Drag and drop files onto it or pass full paths as arguments to be processed.\n";

            string namingNotice =
                "Naming Notice:\n" +
                "Make sure the file name is as it was if you plan to unpack.\n" +
                "Make sure the directory name before any dots is the same name as the file without extensions when repacking.\n" +
                "Example for unpack: AC3DATA.BIN\n" +
                "Example for repack: AC3DATA\n";

            string idNotice =
                "ID Notice:\n" +
                "If you want to set the ID of a file, make sure it's name begins with the ID you wish to set.\n" +
                "The ID will otherwise be attempted to be set by index.\n" +
                "Do not set the ID beyond the range of the supported entry count for your game.\n" +
                "The count - 1 is the max ID to set.\n" +
                "See Supported Notice for the supported entry counts.\n";

            string supportedNotice =
                "Supported Notice:\n" +
                "The following files are supported, their entry count is also shown beside them:\n" +
                $"{Supported}\n" +
                "If you find more please open an issue or let me know some other way.";

            string helpString = $"{guiNotice}\n{namingNotice}\n{idNotice}\n{supportedNotice}";
            Console.WriteLine(helpString);
            Console.ReadKey();
        }
    }
}
