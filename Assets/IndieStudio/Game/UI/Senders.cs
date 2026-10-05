using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>Who (or what) a message comes from: a character with a face, or an app with an icon.</summary>
    public sealed class Sender
    {
        public string Name;
        public AvatarLook Look;
        public Mood Mood;
        public string Icon;
        /// <summary>Background class for the portrait or tile, e.g. "bg-teal".</summary>
        public string Background;

        /// <summary>A square-ish tile of about <paramref name="size"/> pixels: a face, or the app's icon.</summary>
        public VisualElement Portrait(float size, string classes = null)
        {
            if (Look != null) return Ui.Portrait(Look, Mood, size - 10f, Background + " " + classes);
            var tile = Ui.Tile(Icon, Background + " " + classes, size * 0.46f);
            tile.style.width = size;
            tile.style.height = size - 4f;
            return tile;
        }
    }

    /// <summary>Maps simulation events to senders, so the inbox reads like mail from people and apps.</summary>
    public static class Senders
    {
        public static Sender For(Message m)
        {
            switch (m.EventId)
            {
                case "welcome":
                    return Person("Old Dev Doug", AvatarLook.Mentor, Mood.Happy, "bg-mustard-light");
                case "chapter_complete":
                    return Person("Old Dev Doug", AvatarLook.Mentor, Mood.Ecstatic, "bg-mustard");
                case "laptop_overheat":
                case "laptop_dies":
                    return App("Your laptop", Icons.Laptop, "bg-blue");
                case "jam_announcement":
                case "jam_result":
                    return App("Ludum Jam", Icons.Bolt, "bg-yellow");
                case "course_offer":
                    return App("Course sale", Icons.Graduation, "bg-sage-light");
                case "job_offer":
                    return Person("The corner café", AvatarLook.CafeOwner, Mood.Happy, "bg-peach");
                case "old_colleague":
                    return Person("Your old team", AvatarLook.Colleague, Mood.Ecstatic, "bg-teal");
                case "parents_loan":
                    return Person("Mom", AvatarLook.Parent, Mood.Happy, "bg-lavender");
                case "friend_invite":
                    return Person("Your friends", AvatarLook.Friend, Mood.Ecstatic, "bg-cyan");
                case "backup_offer":
                    return App("Cloud backup", Icons.Cloud, "bg-cyan");
                case "streamer_found":
                    return Person("A streamer", AvatarLook.Streamer, Mood.Ecstatic, "bg-pink");
                case "collapse":
                    return Person("You", AvatarLook.Founder, Mood.Panic, "bg-red-light");
                case "vacation_advice":
                case "crisis_of_faith":
                case "noisy_week":
                    return Person("You", AvatarLook.Founder, Mood.Tired, "bg-peach");
                case "job_quit":
                    return Person("You", AvatarLook.Founder, Mood.Ecstatic, "bg-peach");
                case "rent_increase":
                    return Person("Mr. Pennywhistle", AvatarLook.Landlord, Mood.Happy, "bg-cream");
                case "money_warning":
                case "in_red":
                case "monthly_report":
                case "taxes":
                case "game_over":
                    return App("Bank", Icons.Bank, "bg-green");
                case "gig_scope_creep":
                case "client_ghosted":
                case "gig_done":
                case "gig_failed":
                case "gig_late":
                    return App("GigGrid", Icons.Briefcase, "bg-lavender");
                case "research":
                    return App("Idea notebook", Icons.Bulb, "bg-yellow");
                case "project_started":
                case "store_page":
                case "launch":
                case "project_cancelled":
                case "project_complete":
                case "first_sale":
                case "post_took_off":
                case "post_mortem":
                    return App("Your game", Icons.Gamepad, "bg-teal icon-cream");
                case "level_up":
                    return App("Skills", Icons.Star, "bg-pink");
                case "goal":
                    return App("Goals", Icons.Target, "bg-teal icon-cream");
                default:
                    return App("Mail", Icons.Mail, "bg-peach");
            }
        }

        static Sender Person(string name, AvatarLook look, Mood mood, string background) =>
            new Sender { Name = name, Look = look, Mood = mood, Background = background };

        static Sender App(string name, string icon, string background) =>
            new Sender { Name = name, Icon = icon, Background = background };
    }
}
