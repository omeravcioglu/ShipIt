using System;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Day 0 is Monday 1 January of "Year 1". Internally a real calendar is used so months have
    /// real lengths and seasonal events land on sensible dates.
    /// </summary>
    public static class GameCalendar
    {
        // 1 January 2001 was a Monday, which keeps day % 7 == 0 aligned with Mondays.
        static readonly DateTime Origin = new DateTime(2001, 1, 1);

        public static readonly string[] DayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        public static readonly string[] MonthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        public static readonly string[] BlockNames = { "Morning", "Afternoon", "Evening" };

        public static DateTime ToDate(int day) => Origin.AddDays(day);
        public static int Year(int day) => ToDate(day).Year - Origin.Year + 1;
        public static int Month(int day) => ToDate(day).Month;
        public static int DayOfMonth(int day) => ToDate(day).Day;

        /// <summary>0 = Monday … 6 = Sunday.</summary>
        public static int DayOfWeek(int day) => ((day % 7) + 7) % 7;

        public static bool IsMonday(int day) => DayOfWeek(day) == 0;
        public static bool IsWeekend(int day) => DayOfWeek(day) >= 5;
        public static bool IsFirstOfMonth(int day) => DayOfMonth(day) == 1;

        /// <summary>Months elapsed since the start of the game (0 = January of Year 1).</summary>
        public static int MonthIndex(int day) => (Year(day) - 1) * 12 + Month(day) - 1;

        public static int FirstDayOfMonthIndex(int monthIndex) => (int)(Origin.AddMonths(monthIndex) - Origin).TotalDays;

        public static int DaysInMonthIndex(int monthIndex)
        {
            DateTime start = Origin.AddMonths(monthIndex);
            return DateTime.DaysInMonth(start.Year, start.Month);
        }

        public static string MonthLabel(int monthIndex) => $"{MonthNames[monthIndex % 12]} Y{monthIndex / 12 + 1}";

        public static string Format(int day) =>
            $"{DayNames[DayOfWeek(day)]} {DayOfMonth(day)} {MonthNames[Month(day) - 1]}, Year {Year(day)}";

        public static string FormatShort(int day) => $"{DayOfMonth(day)} {MonthNames[Month(day) - 1]} Y{Year(day)}";
    }
}
