using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    public enum MessageKind { Info, Decision, Opportunity, Critical }

    public sealed class MessageOption
    {
        public string Label;
        public string Detail;
    }

    /// <summary>
    /// An inbox item. Only display data and the chosen option are stored; effects are resolved by
    /// event id (see GameSimulation.Events.cs), which keeps messages serializable.
    /// </summary>
    public sealed class Message
    {
        public int Id;
        public int Day;
        public string EventId;
        public int ContextId;
        public MessageKind Kind;
        public string Title;
        public string Body;
        public readonly List<MessageOption> Options = new List<MessageOption>();
        /// <summary>Optional machine-readable id per option (for example which lesson it teaches).</summary>
        public readonly List<string> OptionIds = new List<string>();
        public int DefaultOption;
        public int ExpiresDay = -1;
        public int ChosenOption = -1;
        public bool IsRead;
        public string Outcome;

        public bool NeedsDecision => Options.Count > 0 && ChosenOption < 0;
    }

    public sealed class Inbox
    {
        public readonly List<Message> Messages = new List<Message>();

        public IEnumerable<Message> Pending => Messages.Where(m => m.NeedsDecision);
        public int UnreadCount => Messages.Count(m => !m.IsRead);
        public Message Find(int id) => Messages.FirstOrDefault(m => m.Id == id);
    }
}
