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
            var offset = utf8Line.IndexOf(']');

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

        private static PublicChatMessage ParseGlobalMessage(
            ReadOnlySpan<char> utf8Line, int offset, char prefix,
            DateTime timestamp)
        {
            var msgData = GetMessageData(utf8Line.Slice(offset));
            return new PublicChatMessage(
                msgData.Name,
                msgData.GuildTag,
                msgData.Message,
                prefix, timestamp);
        }

        private static ILogEntry ParseWhisper(
            in ReadOnlySpan<char> data, DateTime timestamp) =>
            data switch
            {
                var x when x.StartsWith("From ") => 
                MessageDataToWhisper(GetMessageData(
                    data.Slice(5)), true, timestamp),

                var x when x.StartsWith("To ") => 
                MessageDataToWhisper(GetMessageData(
                    data.Slice(3)), true, timestamp),

                _ => null
            };

        private static WhisperMessage MessageDataToWhisper(
            IntermediaryMessageData msg, bool incoming, DateTime timestamp) =>
            new WhisperMessage(msg.Name, msg.GuildTag, msg.Message, incoming, timestamp);

        private static IntermediaryMessageData GetMessageData(ReadOnlySpan<char> data)
        {
            ReadOnlySpan<char> tag = default;

            if (data[0] == '<')
            {
                var endTagIndex = data.IndexOf('>');
                tag = data[1..endTagIndex];
                data = data.Slice(endTagIndex + 2);
            }

            var nextSpaceIndex = data.IndexOf(':');
            var name = data[..nextSpaceIndex];
            data = data.Slice(nextSpaceIndex + 2);

            return new IntermediaryMessageData(tag, name, data);
        }

        private static ILogEntry ParseStatus(
            in ReadOnlySpan<char> data, DateTime timestamp) =>
            data switch
            {
                var x when x.StartsWith("Async connecting to ") => 
                new ConnectingToLoginServer(data[20..], timestamp),
                var x when x.StartsWith("Connecting to instance server at ") => 
                new ConnectingToInstance(data[33..], timestamp),
                var x when x.StartsWith("Connect time to instance server was ") => 
                new ConnectedToInstance(int.Parse(data[36..^2]), timestamp),
                var x when x.StartsWith("Connected to ") => 
                ParseConnectedTo(data, timestamp),
                _ => null
            };

        private static ILogEntry ParseConnectedTo(
            ReadOnlySpan<char> data, DateTime timestamp)
        {
            var addressEndIdx = data.Slice(14).IndexOf(' ') + 14;
            var address = data[13..addressEndIdx];
            var msBeginIdx = data.LastIndexOf(' ') + 1;
            var msSpan = data[msBeginIdx..^3];
            var ms = int.Parse(msSpan);
            return new ConnectedToLoginServer(address, ms, timestamp);
        }

        private static ILogEntry ParseColonPrefixed(
            in ReadOnlySpan<char> data, DateTime timestamp) =>
            data switch
            { 
                var x when x.StartsWith("You have entered ") => 
                new YouEnteredArea(data[17..], timestamp) as ILogEntry,
                var x when x.EndsWith(" has joined the area.") => 
                new OtherJoinedArea(data[..^21], timestamp),
                var x when x.EndsWith(" has left the area.") => 
                new OtherLeftArea(data[..^19], timestamp),
                var x when x.EndsWith(" has been slain.") => 
                new SlainMessage(data[..^16], timestamp),
                var x when x.StartsWith("AFK mode is now ON. Autoreply ") =>
                new AfkModeOn(data[31..^1], timestamp),
                var x when x.StartsWith("AFK mode is now OFF") =>
                new AfkModeOff(timestamp),
                var x when x.StartsWith("DND mode is now ON. Autoreply ") =>
                new DndModeOn(data[31..^1], timestamp),
                var x when x.StartsWith("DND mode is now OFF") =>
                new DndModeOff(timestamp),
                var x when x.Contains(" is now level ", StringComparison.Ordinal) =>
                ParseIsNowLevel(data, timestamp),
                _ => null
            };

        private static ILogEntry ParseIsNowLevel(
            ReadOnlySpan<char> data, DateTime timestamp)
        {
            var parIdx = data.IndexOf('(');
            if (parIdx == -1) return null;

            var charName = data[0..--parIdx];
            var lastSpaceIdx = data.LastIndexOf(' ') + 1;
            var level = int.Parse(data[lastSpaceIdx..]);
            return new IsNowLevel(charName, level, timestamp);
        }


        private readonly ref struct IntermediaryMessageData
        {
            public IntermediaryMessageData(
                ReadOnlySpan<char> tag,
                ReadOnlySpan<char> name,
                ReadOnlySpan<char> message)
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
