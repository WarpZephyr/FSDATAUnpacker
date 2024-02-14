using Handlers;
using Helpers;
using Structures;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace FSDATAUnpacker
{
    public class FSDATA
    {
        private static readonly int ENTRY_MEMBER_SIZE = sizeof(int);
        private static readonly int ENTRY_MEMBER_COUNT = 2;
        private static readonly int ENTRY_SIZE = ENTRY_MEMBER_SIZE * ENTRY_MEMBER_COUNT;
        private static readonly int SECTOR_SIZE = 0x800;
        private static readonly int ALIGNMENT_SIZE = 0x1000;

        private int _entry_count;
        private int _base_address => ENTRY_SIZE * EntryCount;

        /// <summary>
        /// How many total entries are in this <see cref="FSDATA"/>.
        /// <para>Not the total file count, but the total entry count.</para>
        /// </summary>
        public int EntryCount
        {
            get => _entry_count;
            set
            {
                if ((value * ENTRY_SIZE) % ALIGNMENT_SIZE != 0)
                {
                    throw new NotSupportedException($"{nameof(EntryCount)} total size must be divisible by {nameof(ALIGNMENT_SIZE)}: {ALIGNMENT_SIZE}");
                }

                _entry_count = value;
            }
        }

        /// <summary>
        /// The file entries in this <see cref="FSDATA"/>.
        /// </summary>
        public List<FileDataInfo> Files { get; set; }

        /// <summary>
        /// Create a <see cref="FSDATA"/> for reading.
        /// </summary>
        /// <param name="entryCount">The number of entries.</param>
        /// <param name="stream">A <see cref="Stream"/> whose position is set at the start of the <see cref="FSDATA"/>.</param>
        public FSDATA(int entryCount, Stream stream)
        {
            EntryCount = entryCount;
            long startPos = stream.Position;
            Files = new List<FileDataInfo>(entryCount);
            using var br = new BinaryReader(stream, Encoding.Default, true);
            for (int i = 0; i < entryCount; i++)
            {
                var entry = ReadFileEntry(br, startPos);
                if (entry.Length > -1)
                {
                    Files.Add(new FileDataInfo(new FileHeader(i.ToString(), i), entry));
                }
            }
        }

        /// <summary>
        /// Create an empty <see cref="FSDATA"/> with a default entry count for writing.
        /// </summary>
        public FSDATA()
        {
            EntryCount = 8192;
            Files = new List<FileDataInfo>(EntryCount);
        }

        /// <summary>
        /// Create a <see cref="FSDATA"/> with the given entry count for writing.
        /// </summary>
        /// <param name="entryCount">The total number of entries in this <see cref="FSDATA"/>.</param>
        public FSDATA(int entryCount)
        {
            EntryCount = entryCount;
            Files = new List<FileDataInfo>(EntryCount);
        }

        /// <summary>
        /// Write this <see cref="FSDATA"/> to a path.
        /// <para>Use this only if all files in the list have valid file paths to get data from.</para>
        /// </summary>
        /// <param name="path">The path to write it to.</param>
        /// <exception cref="IndexOutOfRangeException">An index went out of range while writing null entries.</exception>
        public void Write(string path)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            Write(fs);
        }

        /// <summary>
        /// Write this <see cref="FSDATA"/> to a <see cref="Stream"/>.
        /// <para>Use this only if all files in the list have valid file paths to get data from.</para>
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write it to.</param>
        /// <exception cref="IndexOutOfRangeException">An index went out of range while writing null entries.</exception>
        public void Write(Stream stream)
        {
            long startPos = stream.Position;
            long baseAddress = startPos + _base_address;
            using var bw = new BinaryWriter(stream, Encoding.Default, true);

            // Pre-fill stream to be able to jump to base address
            bw.BaseStream.Fill(EntryCount * ENTRY_SIZE);

            // If there are no files, leave it as an empty archive.
            if (Files.Count < 1)
            {
                return;
            }

            // Jump back to the start position
            bw.BaseStream.Position = startPos;

            // Sort files by ID, generating IDs when necessary beforing sorting
            var processedFiles = ProcessFiles();
            int fileIndex = 0;
            long offset = baseAddress;
            for (int i = 0; i < EntryCount; i++)
            {
                // We've reached the end of the files
                if (fileIndex >= processedFiles.Count)
                {
                    break;
                }

                var file = processedFiles[fileIndex];
                var id = file.FileHeader.ID;

                // Skip null entries (we've already written them)
                while (i != id)
                {
                    if (i >= EntryCount) throw new IndexOutOfRangeException($"Index went out of range; Index: {i}; Max Index: {EntryCount - 1}");
                    bw.BaseStream.Position += ENTRY_SIZE;
                    i++;
                }

                // Get bytes and length 
                PathExceptionHandler.ThrowIfNotFile(file.FileHeader.Path);
                byte[] bytes = File.ReadAllBytes(file.FileHeader.Path);
                long paddedLength = MathHelper.Align(bytes.LongLength, ALIGNMENT_SIZE);

                // Write entry and save the next entry position
                bw.Write((int)((offset - baseAddress) / SECTOR_SIZE));
                bw.Write((int)(paddedLength / SECTOR_SIZE));
                long nextEntryPos = bw.BaseStream.Position;

                // Go to the file offset and write the file along with any padding, then return to write the next entry
                bw.BaseStream.Position = offset;
                bw.Write(bytes);
                bw.BaseStream.Fill(paddedLength - bytes.LongLength);
                bw.BaseStream.Position = nextEntryPos;
                
                // Add the total written length to the next offset and increment the file index
                offset += paddedLength;
                fileIndex++;
            }
        }

        private List<FileDataInfo> ProcessFiles()
        {
            // Prevent files not being written because there isn't enough entries.
            if (Files.Count > EntryCount)
            {
                throw new InvalidOperationException($"{nameof(Files.Count)} cannot be greater than {EntryCount}");
            }

            var processedFiles = new List<FileDataInfo>(Files.Count);
            var usedIDs = new List<int>(EntryCount);
            for (int i = 0; i < Files.Count; i++)
            {
                var file = Files[i];
                int id = file.FileHeader.ID;

                // If the ID is not set, then try to find a suitable ID.
                var processedFile = file;
                if (id == -1)
                {
                    var dataInfo = file.DataHeader;
                    id = GetID(file, i);

                    // If the ID already exists then throw.
                    if (usedIDs.Contains(id))
                        throw new InvalidOperationException($"ID already taken: {id}");
                    usedIDs.Add(id);

                    processedFile = new FileDataInfo(new FileHeader(file.FileHeader.Path, id), file.DataHeader);
                }

                // Prevent the file from not being written because it goes out of range.
                if (id >= EntryCount)
                {
                    throw new IndexOutOfRangeException($"ID out of range; ID: {id}; Max: {EntryCount - 1}");
                }

                processedFiles.Add(processedFile);
            }

            // Sort processed files by ID.
            processedFiles.Sort((x, y) => x.FileHeader.ID.CompareTo(y.FileHeader.ID));
            return processedFiles;
        }

        /// <summary>
        /// Converts file entries into normal offset and length information.
        /// </summary>
        /// <param name="br">A <see cref="BinaryReader"/>.</param>
        /// <param name="streamStartPos">The starting position of the stream so that it may be added to offsets.</param>
        /// <returns>A <see cref="DataHeader"/> object.</returns>
        private DataHeader ReadFileEntry(BinaryReader br, long streamStartPos)
        {
            int startSector = br.ReadInt32();
            int sectorCount = br.ReadInt32();

            if (sectorCount > 0)
            {
                long offset = (_base_address + (startSector * SECTOR_SIZE)) + streamStartPos;
                long length = sectorCount * SECTOR_SIZE;

                return new DataHeader(offset, length);
            }

            return new DataHeader(-1, -1);
        }

        private static int GetID(FileDataInfo file, int index)
        {
            int id = file.FileHeader.ID;

            // If the ID is not set, try to parse the start of the file name as the ID.
            if (id == -1)
            {
                id = ParseID(file.FileHeader.Path);

                // If the start of the file name could not be parsed as the ID, set it as the given index.
                if (id == -1)
                {
                    id = index;
                }
            }
            return id;
        }

        private static int ParseID(string path)
        {
            // Get the file name
            string name = PathHandler.GetFileNameWithoutExtensions(path);

            // If the name is empty or the first char is not a digit, return -1.
            if (string.IsNullOrWhiteSpace(name) || !char.IsDigit(name[0]))
            {
                return -1;
            }

            // Get all the digits at the start of the string.
            var sb = new StringBuilder();
            int index = 0;
            char next = name[index];
            while (char.IsDigit(next))
            {
                sb.Append(next);
                if (index == name.Length - 1)
                {
                    break;
                }

                index++;
                next = name[index];
            }

            // Try to parse the string as an ID.
            string idStr = sb.ToString();
            if (!int.TryParse(idStr, out int id))
            {
                return -1;
            }

            return id;
        }
    }
}
