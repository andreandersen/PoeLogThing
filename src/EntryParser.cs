using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeLogThing
{
    public static class EntryParser
    {
        public static bool TryParse(in ReadOnlySpan<char> utf8Line, out ILogEntry result)
        {
            // Find where the actual information starts
            int offset = utf8Line.IndexOf(']');

            if (utf8Line.Length < 19 ||
                !DateTime.TryParse(utf8Line.Slice(0, 19), out var timestamp) ||
                offset < 19)
            {
                result = null;
                return false;
            }

            offset += 2;
            var prefix = utf8Line[offset];

            offset++;

            result = prefix switch
            {
                var x when x == '#' || x == '%' || x == '$' || x == '&' 
                    => ParseGlobalMessage(utf8Line, offset, prefix, timestamp),
                '@' => ParseWhisper(utf8Line.Slice(offset), timestamp),
                ':' => ParseColonPrefixed(utf8Line.Slice(offset + 1), timestamp),
                _ => ParseStatus(utf8Line.Slice(--offset), timestamp)
            };

            return result != null;
        }

        private static PublicChatMessage ParseGlobalMessage(ReadOnlySpan<char> utf8Line, int offset, char prefix,
            DateTime timestamp)
        {
            var msgData = GetMessageData(utf8Line.Slice(offset));
            return new PublicChatMessage(
                msgData.Name,
                msgData.GuildTag,
                msgData.Message,
                prefix, timestamp);
        }

        private static ILogEntry ParseWhisper(in ReadOnlySpan<char> data, DateTime timestamp)
        {
            int idx = 0;

            if (data.StartsWith("From "))
            {
                var msg = GetMessageData(data.Slice(idx + 5));

                return new WhisperMessage(
                    msg.Name, msg.GuildTag, msg.Message, true, timestamp);
            }
            else if (data.StartsWith("To "))
            {
                var msg = GetMessageData(data.Slice(idx + 3));

                return new WhisperMessage(
                    msg.Name, msg.GuildTag, msg.Message, false, timestamp);
            }

            return null;
        }

        private static IntermediaryMessageData GetMessageData(ReadOnlySpan<char> data)
        {
            ReadOnlySpan<char> tag = default;
            ReadOnlySpan<char> name = default;

            if (data[0] == '<')
            {
                var endTagIndex = data.IndexOf('>');
                tag = data[1..endTagIndex];
                data = data.Slice(endTagIndex + 2);
            }

            var nextSpaceIndex = data.IndexOf(':');
            name = data[..nextSpaceIndex];
            data = data.Slice(nextSpaceIndex + 2);

            return new IntermediaryMessageData(tag, name, data);
        }

        private static ILogEntry ParseStatus(in ReadOnlySpan<char> data, DateTime timestamp)
        {
            if (data.StartsWith("Async connecting to "))
                return new ConnectingToLoginServer(data[20..], timestamp);

            if (data.StartsWith("Connecting to instance server at "))
                return new ConnectingToInstance(data[33..], timestamp);

            if (data.StartsWith("Connect time to instance server was "))
                return new ConnectedToInstance(int.Parse(data[36..^2]), timestamp);

            if (data.StartsWith("Connected to "))
            {
                var addressEndIdx = data.Slice(14).IndexOf(' ') + 14;
                var address = data[13..addressEndIdx];
                var msBeginIdx = data.LastIndexOf(' ') + 1;
                var msSpan = data[msBeginIdx..^3];
                var ms = int.Parse(msSpan);
                return new ConnectedToLoginServer(address, ms, timestamp);
            }

            return null;
        }

        private static ILogEntry ParseColonPrefixed(in ReadOnlySpan<char> data, DateTime timestamp)
        {
            if (data.StartsWith("You have entered "))
                return new YouEnteredArea(data[17..], timestamp);

            if (data.EndsWith(" has joined the area."))
                return new OtherJoinedArea(data[0..^21], timestamp);

            if (data.EndsWith(" has left the area."))
                return new OtherLeftArea(data[0..^19], timestamp);

            if (data.EndsWith(" has been slain."))
                return new SlainMessage(data[0..^16], timestamp);

            if (data.StartsWith("AFK mode is now ON. Autoreply "))
                return new AfkModeOn(data[31..^1], timestamp);

            if (data.StartsWith("AFK mode is now OFF"))
                return new AfkModeOff(timestamp);

            if (data.StartsWith("DND mode is now ON. Autoreply "))
                return new DndModeOn(data[31..^1], timestamp);

            if (data.StartsWith("DND mode is now OFF"))
                return new DndModeOff(timestamp);

            if (data.Contains(" is now level ", StringComparison.Ordinal))
            {
                var parIdx = data.IndexOf('(');
                if (parIdx != -1)
                {
                    var charName = data[0..--parIdx];
                    var lastSpaceIdx = data.LastIndexOf(' ') + 1;
                    int level = int.Parse(data[lastSpaceIdx..]);
                    return new IsNowLevel(charName, level, timestamp);
                }
            }

            return null;
        }


        private readonly ref struct IntermediaryMessageData
        {
            public IntermediaryMessageData(ReadOnlySpan<char> tag, ReadOnlySpan<char> name, ReadOnlySpan<char> message)
            {
                GuildTag = tag;
                Name = name;
                Message = message;
            }

            public ReadOnlySpan<char> GuildTag { get; }
            public ReadOnlySpan<char> Name { get; }
            public ReadOnlySpan<char> Message { get; }
        }
    }
}
