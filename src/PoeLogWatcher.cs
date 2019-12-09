using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoeLogThing
{
    public class LogWatcher
    {

        public async Task WatchAsync(string fileName, CancellationToken ct = default)
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
                    int toRead = (int)(fs.Length - pos);
                    Memory<byte> buffer = new byte[toRead];

                    await fs.ReadAsync(buffer, ct);
                    ProcessBuffer(in buffer);
                    pos = fs.Position;
                }
                await Task.Delay(25);
            }
        }

        private void ProcessBuffer(in Memory<byte> buffer)
        {
            var span = buffer.Span;
            ReadOnlySpan<char> text = Encoding.UTF8.GetString(span);

            while (!text.IsEmpty)
            {
                var lineBreak = text.IndexOf("\r\n");
                if (lineBreak == -1) break;

                var line = text.Slice(0, lineBreak);

                // Emit your event here instead of Console
                Console.WriteLine(line.ToString());

                //if (!EntryParser.TryParse(in line, out var logMessage))
                //{
                //    //Debug.WriteLine("Failed to parse");
                //}

                text = text.Slice(lineBreak + 2);
            }
        }
    }
}
