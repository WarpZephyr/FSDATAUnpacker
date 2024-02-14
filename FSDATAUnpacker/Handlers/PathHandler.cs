namespace Handlers
{
    internal static class PathHandler
    {
        internal static string GetDirectoryName(string path)
        {
            string? directory = Path.GetDirectoryName(path) ?? path;
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException($"Provided path did not contain directory information: {path}");
            }

            return directory;
        }

        internal static string CorrectDirectorySeparatorChar(string path) => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        internal static string TrimLeadingDirectorySeparators(string path) => path.TrimStart('\\', '/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        internal static string TrimTrailingDirectorySeparators(string path) => path.TrimEnd('\\', '/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        internal static string CleanPath(string path) => CorrectDirectorySeparatorChar(TrimLeadingDirectorySeparators(path));

        internal static string Combine(string path1, string path2) => Path.Combine(CleanPath(path1), CleanPath(path2));

        internal static string GetWithoutExtensions(string path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            int index = path.IndexOf('.');
            if (index > -1)
            {
                return path[..index];
            }
            return path;
        }

        internal static string GetFileNameWithoutExtensions(string path) => GetWithoutExtensions(Path.GetFileName(path));

        internal static string GetDirectoryNameWithoutPath(string path) => Path.GetFileName(TrimTrailingDirectorySeparators(path));
    }
}
