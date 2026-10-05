using System;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// A day-by-day cash projection built only from what the bank already knows: money owed to you on its
    /// due date, and your monthly bills (and day-job salary) on the 1st. The hopeful line adds gig income
    /// and game sales at their recent pace, so the gap between the two is honest uncertainty (Design/13 §1).
    /// </summary>
    public sealed class CashForecast
    {
        public double[] Known;
        public double[] Hopeful;
        /// <summary>First index where the known line goes below zero, or -1.</summary>
        public int BrokeIndex = -1;
        public double GigsPerWeek;
        public double SalesPerMonth;
        public int StartDay;

        /// <summary>The day whose events produced value <paramref name="index"/> (index 0 is right now).</summary>
        public int DayAt(int index) => StartDay + Math.Max(0, index - 1);

        public static CashForecast Build(GameState state, int days)
        {
            var fin = state.Finance;
            var forecast = new CashForecast
            {
                Known = new double[days + 1],
                Hopeful = new double[days + 1],
                StartDay = state.Day,
                GigsPerWeek = Math.Max(0, fin.Transactions.Where(t => t.Category == TxCategory.Freelance && t.Day >= state.Day - 28).Sum(t => t.Amount) / 4.0),
                SalesPerMonth = Math.Max(0, fin.Transactions.Where(t => t.Category == TxCategory.GameSales && t.Day >= state.Day - 60).Sum(t => t.Amount) / 2.0),
            };
            double salary = state.Founder.Job == DayJob.PartTime ? Tuning.PartTimeMonthlyPay
                          : state.Founder.Job == DayJob.FullTime ? Tuning.FullTimeMonthlyPay : 0;
            double known = fin.Cash, extra = 0;
            forecast.Known[0] = forecast.Hopeful[0] = known;
            for (int i = 1; i <= days; i++)
            {
                int day = state.Day + i - 1;
                foreach (var payment in fin.Scheduled)
                    if (payment.Day == day || (i == 1 && payment.Day < day)) known += payment.Amount;
                if (day > 0 && GameCalendar.IsFirstOfMonth(day)) known += salary - fin.MonthlyRecurringTotal;
                if (i > 7) extra += forecast.GigsPerWeek / 7.0;
                if (i > 30) extra += forecast.SalesPerMonth / 30.0;
                forecast.Known[i] = known;
                forecast.Hopeful[i] = known + extra;
                if (forecast.BrokeIndex < 0 && known < 0) forecast.BrokeIndex = i;
            }
            return forecast;
        }

        /// <summary>A rounded axis range that always shows zero.</summary>
        public void Range(out double min, out double max)
        {
            double hi = Math.Max(Hopeful.Max(), 1000);
            double lo = Math.Min(Known.Min(), 0);
            double step = hi - lo > 40000 ? 10000 : hi - lo > 12000 ? 5000 : hi - lo > 4000 ? 2000 : 1000;
            max = Math.Ceiling(hi * 1.08 / step) * step;
            min = lo < 0 ? Math.Floor(lo * 1.1 / step) * step : -step * 0.25;
        }
    }

    /// <summary>
    /// Draws a forecast: the known cash line as steps, a band up to the hopeful line, a dashed zero line
    /// with a tinted "out of money" zone, and a marker where the known line crosses zero.
    /// </summary>
    public sealed class ForecastChart : VisualElement
    {
        static readonly Color Band = Palette.Hex("#F4B63F", 0.45f);
        static readonly Color Danger = Palette.Hex("#C73A2E", 0.10f);

        double[] _known = Array.Empty<double>();
        double[] _hopeful = Array.Empty<double>();
        double _min, _max = 1;
        int _mark = -1;
        float _lineWidth = 3.5f;

        public ForecastChart()
        {
            AddToClassList("forecast-chart");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void SetData(CashForecast forecast, double min, double max, float lineWidth = 3.5f)
        {
            _known = forecast.Known;
            _hopeful = forecast.Hopeful;
            _min = min;
            _max = Math.Abs(max - min) < 1e-6 ? min + 1 : max;
            _mark = forecast.BrokeIndex;
            _lineWidth = lineWidth;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            int n = _known.Length - 1;
            if (rect.width < 1f || rect.height < 1f || n < 1) return;
            float w = rect.width, h = rect.height;
            float X(int i) => i / (float)n * w;
            float Y(double v) => (float)((_max - v) / (_max - _min)) * h;
            var p = context.painter2D;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;

            float zero = Y(0);
            if (zero < h)
            {
                p.fillColor = Danger;
                p.BeginPath();
                p.MoveTo(new Vector2(0, zero));
                p.LineTo(new Vector2(w, zero));
                p.LineTo(new Vector2(w, h));
                p.LineTo(new Vector2(0, h));
                p.ClosePath();
                p.Fill();
            }

            p.fillColor = Band;
            p.BeginPath();
            p.MoveTo(new Vector2(0, Y(_hopeful[0])));
            for (int i = 1; i <= n; i++)
            {
                p.LineTo(new Vector2(X(i), Y(_hopeful[i - 1])));
                p.LineTo(new Vector2(X(i), Y(_hopeful[i])));
            }
            for (int i = n; i >= 1; i--)
            {
                p.LineTo(new Vector2(X(i), Y(_known[i])));
                p.LineTo(new Vector2(X(i), Y(_known[i - 1])));
            }
            p.ClosePath();
            p.Fill();

            Dashed(p, new Vector2(0, zero), new Vector2(w, zero), Palette.Red, 2.5f);

            p.strokeColor = Palette.Ink;
            p.lineWidth = _lineWidth;
            p.BeginPath();
            p.MoveTo(new Vector2(0, Y(_known[0])));
            for (int i = 1; i <= n; i++)
            {
                p.LineTo(new Vector2(X(i), Y(_known[i - 1])));
                p.LineTo(new Vector2(X(i), Y(_known[i])));
            }
            p.Stroke();

            if (_mark > 0)
            {
                Dashed(p, new Vector2(X(_mark), 0), new Vector2(X(_mark), h), Palette.Red, 2.5f);
                Dot(p, new Vector2(X(_mark), zero), _lineWidth * 2.2f, Palette.Red);
            }
            Dot(p, new Vector2(Mathf.Max(_lineWidth * 2f, 1f), Y(_known[0])), _lineWidth * 2f, Palette.Tomato);
        }

        static void Dashed(Painter2D p, Vector2 from, Vector2 to, Color color, float width)
        {
            float length = Vector2.Distance(from, to);
            if (length < 1f) return;
            var direction = (to - from) / length;
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            for (float t = 0; t < length; t += 13f)
            {
                p.MoveTo(from + direction * t);
                p.LineTo(from + direction * Mathf.Min(length, t + 7f));
            }
            p.Stroke();
        }

        static void Dot(Painter2D p, Vector2 center, float radius, Color fill)
        {
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.fillColor = fill;
            p.Fill();
            p.strokeColor = Palette.Ink;
            p.lineWidth = Mathf.Max(2f, radius * 0.4f);
            p.Stroke();
        }
    }

    /// <summary>The torn zigzag bottom of a paper receipt.</summary>
    public sealed class ZigzagEdge : VisualElement
    {
        public ZigzagEdge()
        {
            AddToClassList("zigzag");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;
            float w = rect.width, h = rect.height, inset = 1.5f;
            int teeth = Mathf.Max(2, Mathf.RoundToInt((w - 2 * inset) / 22f));
            float tooth = (w - 2 * inset) / teeth;
            float top = h * 0.25f, bottom = h - 2f;

            var p = context.painter2D;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(new Vector2(inset, -1f));
            p.LineTo(new Vector2(w - inset, -1f));
            p.LineTo(new Vector2(w - inset, top));
            for (int i = teeth - 1; i >= 0; i--)
            {
                p.LineTo(new Vector2(inset + (i + 0.5f) * tooth, bottom));
                p.LineTo(new Vector2(inset + i * tooth, top));
            }
            p.ClosePath();
            p.fillColor = Palette.Paper;
            p.Fill();

            p.BeginPath();
            p.MoveTo(new Vector2(inset, -2f));
            p.LineTo(new Vector2(inset, top));
            for (int i = 0; i < teeth; i++)
            {
                p.LineTo(new Vector2(inset + (i + 0.5f) * tooth, bottom));
                p.LineTo(new Vector2(inset + (i + 1) * tooth, top));
            }
            p.LineTo(new Vector2(w - inset, -2f));
            p.strokeColor = Palette.Ink;
            p.lineWidth = 3f;
            p.Stroke();
        }
    }

    /// <summary>A cat, asleep on the rug. Pet it when you're feeling low (once a day); it wakes up for the rest of the day.</summary>
    public sealed class Cat : VisualElement
    {
        const string Tail = "M18 70 C10 70 6 60 14 56 C30 48 20 30 34 28";
        const string Ears = "M78 34 L80 18 L90 28 Z M96 28 L106 18 L108 34 Z";
        const string Eyes = "M84 44 Q88 48 92 44 M96 44 Q100 48 104 44";
        static readonly string Body = SvgPath.Ellipse(62, 58, 36, 18);
        static readonly string Head = SvgPath.Circle(92, 44, 18);
        static readonly Color Fur = Palette.Hex("#F4A259");
        bool _awake;

        public Cat()
        {
            AddToClassList("cat");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        /// <summary>Eyes open (it has been petted today).</summary>
        public bool Awake
        {
            get => _awake;
            set
            {
                if (_awake == value) return;
                _awake = value;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;
            float scale = Mathf.Min(rect.width / 120f, rect.height / 80f);
            var offset = Vector2.zero;
            var p = context.painter2D;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            Stroke(p, Tail, Palette.Ink, 10f * scale, scale, offset);
            Stroke(p, Tail, Fur, 4f * scale, scale, offset);
            Shape(p, Body, scale, offset);
            Shape(p, Head, scale, offset);
            Shape(p, Ears, scale, offset);
            if (_awake)
            {
                Dot(p, new Vector2(88f, 44f) * scale + offset, 3f * scale);
                Dot(p, new Vector2(100f, 44f) * scale + offset, 3f * scale);
            }
            else Stroke(p, Eyes, Palette.Ink, 3f * scale, scale, offset);
        }

        static void Dot(Painter2D p, Vector2 center, float radius)
        {
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.fillColor = Palette.Ink;
            p.Fill();
        }

        static void Shape(Painter2D p, string path, float scale, Vector2 offset)
        {
            p.BeginPath();
            SvgPath.Trace(p, path, scale, offset);
            p.fillColor = Fur;
            p.Fill();
            p.strokeColor = Palette.Ink;
            p.lineWidth = 4f * scale;
            p.Stroke();
        }

        static void Stroke(Painter2D p, string path, Color color, float width, float scale, Vector2 offset)
        {
            p.BeginPath();
            SvgPath.Trace(p, path, scale, offset);
            p.strokeColor = color;
            p.lineWidth = width;
            p.Stroke();
        }
    }

    /// <summary>Steam curling up from a fresh mug of coffee.</summary>
    public sealed class Steam : VisualElement
    {
        static readonly string[] Curls = { "M7 34 C1 27 13 21 7 14 C3 10 9 5 7 1", "M19 34 C13 27 25 21 19 14 C15 10 21 5 19 1" };
        bool _up;

        public Steam()
        {
            AddToClassList("steam");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            schedule.Execute(() =>
            {
                _up = !_up;
                EnableInClassList("steam--up", _up);
            }).Every(800);
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;
            float scale = Mathf.Min(rect.width / 26f, rect.height / 35f);
            var p = context.painter2D;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            foreach (var curl in Curls)
            {
                p.BeginPath();
                SvgPath.Trace(p, curl, scale, Vector2.zero);
                p.strokeColor = Palette.Ink;
                p.lineWidth = 6.5f * scale;
                p.Stroke();
                p.BeginPath();
                SvgPath.Trace(p, curl, scale, Vector2.zero);
                p.strokeColor = Palette.Paper;
                p.lineWidth = 3f * scale;
                p.Stroke();
            }
        }
    }
}
