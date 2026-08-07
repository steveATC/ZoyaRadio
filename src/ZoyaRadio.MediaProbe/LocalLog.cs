using System.Globalization;
using System.Text;

namespace ZoyaRadio.MediaProbe;

internal sealed class LocalLog : IDisposable
{
    private readonly object _writeLock = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    private LocalLog(string filePath, StreamWriter writer)
    {
        FilePath = filePath;
        _writer = writer;
    }

    public string FilePath { get; }

    public static LocalLog Create()
    {
        string repositoryRoot = FindRepositoryRoot();
        string logDirectory = Path.Combine(repositoryRoot, "logs");
        Directory.CreateDirectory(logDirectory);

        string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string filePath = Path.Combine(logDirectory, $"media-probe-{timestamp}.txt");
        var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };

        return new LocalLog(filePath, writer);
    }

    public void Write(string message)
    {
        string singleLineMessage = message.ReplaceLineEndings(" ").TrimEnd();
        string line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {singleLineMessage}";

        lock (_writeLock)
        {
            if (_disposed)
            {
                return;
            }

            Console.WriteLine(line);
            _writer.WriteLine(line);
        }
    }

    public void Warning(string context, Exception exception)
    {
        Write($"WARNING: {context}. {exception.GetType().Name}: {exception.Message}");
    }

    private static string FindRepositoryRoot()
    {
        foreach (string startingPath in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startingPath);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "global.json"))
                    && Directory.Exists(Path.Combine(directory.FullName, "src")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return Environment.CurrentDirectory;
    }

    public void Dispose()
    {
        lock (_writeLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _writer.Dispose();
        }
    }
}
