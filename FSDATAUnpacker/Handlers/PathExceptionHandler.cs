using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Handlers
{
    internal static class PathExceptionHandler
    {
        internal static void ThrowIfNotFile([NotNull] string? filePath, [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, paramName);
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"A file did not exist at: {filePath}", paramName);
            }
        }

        internal static void ThrowIfNotDirectory([NotNull] string? directoryPath, [CallerArgumentExpression(nameof(directoryPath))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath, paramName);
            if (!Directory.Exists(directoryPath))
            {
                throw new ArgumentException($"A directory did not exist at: {directoryPath}", paramName);
            }
        }

        internal static void ThrowIfFile([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (File.Exists(path))
            {
                throw new ArgumentException($"Must not be file at: {path}", paramName);
            }
        }

        internal static void ThrowIfDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (Directory.Exists(path))
            {
                throw new ArgumentException($"Must not be directory at: {path}", paramName);
            }
        }
    }
}
