using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoeLogThing
{
    public class LogWatcher
    {

        public async IAsyncEnumerable<ILogEntry> WatchAsync(
            string fileName,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await using var fs = new FileStream(
                fileName, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite, 4096, true);

            fs.Seek(0, SeekOrigin.End);
            var pos = fs.Position;

            while (!ct.IsCancellationRequested)
            {
                if (fs.Length != pos)
                {
                    var toRead = (int)(fs.Length - pos);
                    Memory<byte> buffer = new byte[toRead];

                    await fs.ReadAsync(buffer, ct);
                    foreach (var l in ProcessBuffer(buffer))
                    {
                        yield return l;
                    }

                    pos = fs.Position;
                }

                await Task.Delay(25, ct);
            }
        }

        private static IEnumerable<ILogEntry> ProcessBuffer(Memory<byte> buffer)
        {
            var span = buffer.Span;
            ReadOnlySpan<char> text = Encoding.UTF8.GetString(span);

            var list = new List<ILogEntry>();
            while (!text.IsEmpty)
            {
                var lineBreak = text.IndexOf("\r\n");
                if (lineBreak == -1) break;

                var line = text.Slice(0, lineBreak);

                if (EntryParser.TryParse(line, out var entry))
                    list.Add(entry);

                text = text.Slice(lineBreak + 2);
            }

            return list;
        }
    }
}
