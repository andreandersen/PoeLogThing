using System;

namespace PoeLogThing
{
    public interface ILogEntry
    {
        public DateTime Timestamp { get; }
    }

    public abstract class LogEntry : ILogEntry
    {
        public LogEntry(in DateTime timestamp)
        {
            Timestamp = timestamp;
        }

        public virtual DateTime Timestamp { get; }
        public override string ToString() => $"Generic message at {Timestamp}";
    }


    public class MessageEntry : LogEntry
    {
        public MessageEntry(
            in ReadOnlySpan<char> guildTag,
            in ReadOnlySpan<char> charName,
            in ReadOnlySpan<char> message,
            in DateTime timestamp) : base(timestamp)
        {
            GuildTag = guildTag.ToString();
            Character = charName.ToString();
            Message = message.ToString();
        }

        public string GuildTag { get; protected set; }
        public string Character { get; protected set; }
        public string Message { get; protected set; }

        public override string ToString() => !string.IsNullOrEmpty(GuildTag)
            ? $"[{Timestamp}] {Character} <{GuildTag}>: {Message}"
            : $"[{Timestamp}] {Character}: {Message}";
    }

    public class WhisperMessage : MessageEntry
    {
        public WhisperMessage(
            in ReadOnlySpan<char> charName,
            in ReadOnlySpan<char> guildTag,
            in ReadOnlySpan<char> message,
            in bool isIncoming,
            in DateTime timestamp) : base(guildTag, charName, message, timestamp)
        {
            IsIncoming = isIncoming;
        }

        public bool IsIncoming { get; }
    }

    public class PublicChatMessage : MessageEntry
    {
        public PublicChatMessage(
            in ReadOnlySpan<char> charName,
            in ReadOnlySpan<char> guildTag,
            in ReadOnlySpan<char> message,
            in char prefix,
            in DateTime timestamp) : base(guildTag, charName, message, timestamp)
        {
            MessageType = prefix switch
            {
                '#' => PublicChatMessageType.Global,
                '$' => PublicChatMessageType.Trade,
                '%' => PublicChatMessageType.Party,
                '&' => PublicChatMessageType.Guild,
                _ => PublicChatMessageType.Unknown
            };
        }

        public PublicChatMessageType MessageType { get; }
    }

    public class SlainMessage : LogEntry
    {
        public SlainMessage(
            in ReadOnlySpan<char> charName, DateTime timestamp)
            : base(timestamp)
        {
            CharacterName = charName.ToString();
        }

        public string CharacterName { get; }
    }

    public class IsNowLevel : LogEntry
    {
        public IsNowLevel(
            in ReadOnlySpan<char> charName,
            int level, DateTime timestamp) : base(timestamp)
        {
            CharacterName = charName.ToString();
            Level = level;
        }

        public string CharacterName { get; }
        public int Level { get; }
    }

    public class OtherJoinedArea : LogEntry
    {
        public OtherJoinedArea(
            in ReadOnlySpan<char> charName,
            DateTime timestamp) : base(timestamp)
        {
            CharacterName = charName.ToString();
        }

        public string CharacterName { get; }
    }

    public class YouEnteredArea : LogEntry
    {
        public YouEnteredArea(
            in ReadOnlySpan<char> area,
            DateTime timestamp) : base(timestamp)
        {
            Area = area[0..^1].ToString();

            if (area.EndsWith("Hideout."))
                IsHideout = true;
        }
        public bool IsHideout { get; }
        public string Area { get; }
    }

    public class OtherLeftArea : LogEntry
    {
        public OtherLeftArea(
            in ReadOnlySpan<char> charName,
            DateTime timestamp) : base(timestamp)
        {
            CharacterName = charName.ToString();
        }

        public string CharacterName { get; }
    }

    public class ConnectingToLoginServer : LogEntry
    {
        public ConnectingToLoginServer(
            in ReadOnlySpan<char> addressPort,
            DateTime timestamp) : base(timestamp)
        {
            var portIndex = addressPort.IndexOf(':');
            Address = addressPort[0..portIndex].ToString();
            Port = int.Parse(addressPort[++portIndex..]);
        }

        public string Address { get; }
        public int Port { get; }
    }

    public class ConnectedToLoginServer : LogEntry
    {
        public ConnectedToLoginServer(
            in ReadOnlySpan<char> address,
            int ms,
            DateTime timestamp) : base(timestamp)
        {
            Address = address.ToString();
            TimeInMilliseconds = ms;
        }

        public string Address { get; }
        public int TimeInMilliseconds { get; }
    }

    public class ConnectingToInstance : LogEntry
    {
        public ConnectingToInstance(
            ReadOnlySpan<char> addressPort, DateTime timestamp)
            : base(timestamp)
        {
            var portIndex = addressPort.IndexOf(':');
            IpAddress = addressPort[0..portIndex].ToString();
            Port = int.Parse(addressPort[++portIndex..]);
        }

        public string IpAddress { get; }
        public int Port { get; }
    }

    public class ConnectedToInstance : LogEntry
    {
        public ConnectedToInstance(
            int ms,
            DateTime timestamp) : base(timestamp)
        {
            TimeInMilliseconds = ms;
        }

        public int TimeInMilliseconds { get; }
    }

    public class AfkModeOn : LogEntry
    {
        public AfkModeOn(
            in ReadOnlySpan<char> afkMessage,
            DateTime timestamp) : base(timestamp)
        {
            AutoReplyMessage = afkMessage.ToString();
        }

        public string AutoReplyMessage { get; }
    }

    public class AfkModeOff : LogEntry
    {
        public AfkModeOff(DateTime timestamp) : base(timestamp) { }
    }

    public class DndModeOn : LogEntry
    {
        public DndModeOn(
            in ReadOnlySpan<char> message,
            DateTime timestamp) : base(timestamp)
        {
            AutoReplyMessage = message.ToString();
        }

        public string AutoReplyMessage { get; }
    }

    public class DndModeOff : LogEntry
    {
        public DndModeOff(DateTime timestamp) : base(timestamp) { }
    }

    public enum PublicChatMessageType
    {
        Global,
        Trade,
        Party,
        Guild,
        Unknown
    }
}

