using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    public enum InboxFolder { All, Decisions, Critical, Opportunities, News }

    /// <summary>
    /// Mail (Design/18 §6): folders by severity, the message list, and a reading pane where decisions are
    /// answered. Decisions and critical messages also pop up as a modal and pause the game.
    /// </summary>
    public sealed class InboxPage : Page
    {
        static readonly (InboxFolder Folder, string Name, string Icon, string Classes)[] FolderDefs =
        {
            (InboxFolder.All, "All mail", Icons.Mail, "bg-peach"),
            (InboxFolder.Decisions, "Decisions", Icons.Scale, "kind-decision"),
            (InboxFolder.Critical, "Critical", Icons.Warning, "kind-critical"),
            (InboxFolder.Opportunities, "Opportunities", Icons.Star, "kind-opportunity"),
            (InboxFolder.News, "News", Icons.Info, "kind-info"),
        };

        int _selectedId = -1;
        InboxFolder _folder = InboxFolder.All;

        public InboxPage(StudioUI studio) : base(studio) { }

        public override string Title => "Mail";
        public override string IconPath => Icons.Mail;
        public override string AppClass => "app-mail";

        public void SetFolder(InboxFolder folder)
        {
            _folder = folder;
            _selectedId = -1;
        }

        public void Select(int messageId)
        {
            _folder = InboxFolder.All;
            _selectedId = messageId;
        }

        static bool InFolder(Message m, InboxFolder folder)
        {
            switch (folder)
            {
                case InboxFolder.Decisions: return m.Options.Count > 0;
                case InboxFolder.Critical: return m.Kind == MessageKind.Critical;
                case InboxFolder.Opportunities: return m.Kind == MessageKind.Opportunity;
                case InboxFolder.News: return m.Kind == MessageKind.Info && m.Options.Count == 0;
                default: return true;
            }
        }

        public override void Refresh()
        {
            Root.Clear();
            var inbox = State.Inbox;
            Header("Mail", null);
            BarNote = inbox.UnreadCount > 0 ? $"{inbox.UnreadCount} unread" : "All caught up";

            var messages = inbox.Messages.Where(m => InFolder(m, _folder)).Reverse().Take(80).ToList();
            if (messages.All(m => m.Id != _selectedId))
                _selectedId = (messages.FirstOrDefault(m => m.NeedsDecision) ?? messages.FirstOrDefault())?.Id ?? -1;
            var selected = inbox.Find(_selectedId);

            var columns = Ui.Row("inbox");
            Root.Add(columns);
            columns.Add(Folders());
            columns.Add(List(messages));
            columns.Add(Reader(selected));
        }

        VisualElement Folders()
        {
            var column = Ui.El("inbox__folders");
            foreach (var def in FolderDefs)
            {
                var folder = def.Folder;
                var messages = State.Inbox.Messages.Where(m => InFolder(m, folder)).ToList();
                int count = folder == InboxFolder.Decisions ? messages.Count(m => m.NeedsDecision) : messages.Count(m => !m.IsRead);
                var button = Ui.Clickable(() =>
                {
                    SetFolder(folder);
                    Refresh();
                }, "folder" + (folder == _folder ? " folder--active" : ""));
                button.Add(Ui.Tile(def.Icon, "folder__tile " + def.Classes, 16f));
                button.Add(Ui.Text(def.Name, "folder__name"));
                if (count > 0) button.Add(Ui.Text(count.ToString(), "folder__count"));
                column.Add(button);
            }
            var markAll = Command("Mark all read", () =>
            {
                foreach (var m in State.Inbox.Messages) Sim.MarkRead(m.Id);
            }, "btn btn--small btn--mustard");
            column.Add(markAll);
            column.Add(Ui.Grow());

            var rules = Ui.Card("Pausing", "inbox__rules");
            rules.Add(Rule(Icons.Lock, "Critical", "Always pauses the game", "bg-red-light"));
            rules.Add(Rule(Icons.Lock, "Decisions", "Pause until you answer", "bg-mustard-light"));
            rules.Add(Rule(Icons.Info, "Everything else", "Pops up for 5 seconds", "bg-cream"));
            column.Add(rules);
            return column;
        }

        static VisualElement Rule(string icon, string name, string text, string classes)
        {
            var row = Ui.Row("rule " + classes);
            var words = Ui.El("grow");
            words.Add(Ui.Text(name, "rule__name"));
            words.Add(Ui.Text(text, "rule__text"));
            row.Add(words);
            row.Add(new Icon(icon, 16f));
            return row;
        }

        VisualElement List(List<Message> messages)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("inbox__list");
            if (messages.Count == 0) scroll.Add(Ui.Empty("Nothing here. Enjoy the quiet while it lasts."));
            foreach (var m in messages)
            {
                var sender = Senders.For(m);
                var item = Ui.Row("mail" + (m.Id == _selectedId ? " mail--selected" : "") + (m.IsRead ? "" : " mail--unread"));
                item.Add(sender.Portrait(48f, "mail__tile"));
                var text = Ui.El("mail__text");
                var top = Ui.Row("mail__top");
                top.Add(Ui.Text(sender.Name, "mail__from"));
                top.Add(Ui.Chip(Fmt.KindLabel(m.Kind), "chip--kind " + Fmt.KindClass(m.Kind)));
                top.Add(Ui.Grow());
                top.Add(Ui.Text(GameCalendar.FormatShort(m.Day), "mail__date"));
                text.Add(top);
                var titleRow = Ui.Row("mail__title-row");
                if (!m.IsRead) titleRow.Add(Ui.El("unread-dot"));
                titleRow.Add(Ui.Text(m.Title, "mail__title"));
                text.Add(titleRow);
                var bottom = Ui.Row("mail__bottom");
                bottom.Add(Ui.Text(FirstLine(m.Body), "mail__snippet"));
                if (m.NeedsDecision) bottom.Add(Ui.Pill(Icons.Clock, m.ExpiresDay >= 0 ? "by " + GameCalendar.FormatShort(m.ExpiresDay) : "decide", "pill--ink"));
                else if (m.ChosenOption >= 0) bottom.Add(Ui.Pill(Icons.Check, "Done", "pill--good"));
                text.Add(bottom);
                item.Add(text);
                int id = m.Id;
                item.RegisterCallback<ClickEvent>(e =>
                {
                    _selectedId = id;
                    Studio.Do(() => Sim.MarkRead(id));
                });
                scroll.Add(item);
            }
            return scroll;
        }

        static string FirstLine(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            int newline = body.IndexOf('\n');
            string line = newline >= 0 ? body.Substring(0, newline) : body;
            return line.Length > 90 ? line.Substring(0, 87) + "…" : line;
        }

        VisualElement Reader(Message m)
        {
            var reader = Ui.El("reader");
            if (m == null)
            {
                reader.Add(Ui.Empty("Select a message."));
                return reader;
            }
            var sender = Senders.For(m);
            var head = Ui.Row("reader__head reader__head--" + m.Kind.ToString().ToLowerInvariant());
            head.Add(sender.Portrait(92f, "reader__portrait"));
            var info = Ui.El("grow");
            var chips = Ui.Row("reader__chips");
            chips.Add(Ui.Pill(Fmt.KindIcon(m.Kind), Fmt.KindLabel(m.Kind), "pill--kind " + Fmt.KindClass(m.Kind)));
            if (m.NeedsDecision && m.ExpiresDay >= 0) chips.Add(Ui.Pill(Icons.Clock, "Decide by " + GameCalendar.FormatShort(m.ExpiresDay), "pill--ink"));
            chips.Add(Ui.Text(GameCalendar.Format(m.Day), "reader__date"));
            info.Add(chips);
            info.Add(Ui.Display(sender.Name, "reader__from"));
            head.Add(info);
            reader.Add(head);

            var body = new ScrollView(ScrollViewMode.Vertical);
            body.AddToClassList("reader__body");
            body.Add(Ui.Display(m.Title, "reader__title"));
            var letter = Ui.El("letter");
            letter.Add(Ui.Text(m.Body, "letter__text"));
            if (m.NeedsDecision || (m.Kind == MessageKind.Critical && m.ChosenOption < 0))
                letter.Add(Ui.Display(m.NeedsDecision ? "YOUR MOVE" : "GAME PAUSED", "stamp " + (m.Kind == MessageKind.Critical ? "stamp--red" : "stamp--brown")));
            body.Add(letter);

            if (m.NeedsDecision)
            {
                body.Add(Ui.Text("WHAT WILL YOU DO?", "card__title"));
                for (int i = 0; i < m.Options.Count; i++)
                {
                    int option = i;
                    var o = m.Options[i];
                    var button = Ui.Clickable(() => Studio.Do(() => Sim.ChooseOption(m.Id, option)), "choice choice--wide" + (i == 0 ? " choice--primary" : ""));
                    button.Add(Ui.Text(o.Label, "choice__label"));
                    if (!string.IsNullOrEmpty(o.Detail)) button.Add(Ui.Text(o.Detail, "choice__detail"));
                    body.Add(button);
                }
                if (m.ExpiresDay >= 0)
                    body.Add(Ui.Text($"No answer by {GameCalendar.FormatShort(m.ExpiresDay)} means \"{m.Options[m.DefaultOption].Label}\".", "small muted"));
            }
            else if (m.ChosenOption >= 0)
            {
                var done = Ui.Row("outcome");
                done.Add(new Icon(Icons.Check, 20f, 3f));
                var words = Ui.El("grow");
                words.Add(Ui.Text("You chose: " + m.Options[m.ChosenOption].Label, "outcome__choice"));
                if (!string.IsNullOrEmpty(m.Outcome)) words.Add(Ui.Text(m.Outcome, "outcome__text"));
                done.Add(words);
                body.Add(done);
            }
            reader.Add(body);
            return reader;
        }
    }
}
