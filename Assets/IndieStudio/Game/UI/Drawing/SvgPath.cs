using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Traces SVG path data onto a Painter2D, so icons and characters are drawn from the same path strings
    /// as the design mockups. Supports the absolute commands M L H V C Q A Z (arcs must be circular).
    /// Parsed paths are cached by their string.
    /// </summary>
    public static class SvgPath
    {
        enum Op { Move, Line, Cubic, Arc, Close }

        struct Segment
        {
            public Op Op;
            public Vector2 A, B, C;          // Move/Line: A. Cubic: A, B (controls), C (end). Arc: A (center).
            public float Radius, From, To;   // arcs, in radians
            public bool Clockwise;
        }

        static readonly Dictionary<string, Segment[]> Cache = new Dictionary<string, Segment[]>();

        public static void Trace(Painter2D painter, string data, float scale, Vector2 offset)
        {
            foreach (var s in Parse(data))
            {
                switch (s.Op)
                {
                    case Op.Move: painter.MoveTo(offset + s.A * scale); break;
                    case Op.Line: painter.LineTo(offset + s.A * scale); break;
                    case Op.Cubic: painter.BezierCurveTo(offset + s.A * scale, offset + s.B * scale, offset + s.C * scale); break;
                    case Op.Arc:
                        painter.Arc(offset + s.A * scale, s.Radius * scale, Angle.Radians(s.From), Angle.Radians(s.To),
                            s.Clockwise ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);
                        break;
                    case Op.Close: painter.ClosePath(); break;
                }
            }
        }

        /// <summary>A circle as path data (two half arcs).</summary>
        public static string Circle(float cx, float cy, float r) =>
            string.Format(CultureInfo.InvariantCulture, "M{0} {1} A{2} {2} 0 1 0 {3} {1} A{2} {2} 0 1 0 {0} {1} Z", cx - r, cy, r, cx + r);

        /// <summary>An ellipse as path data (four cubic curves).</summary>
        public static string Ellipse(float cx, float cy, float rx, float ry)
        {
            const float k = 0.5523f;
            float ox = rx * k, oy = ry * k;
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "M{0} {1} ", cx - rx, cy);
            sb.AppendFormat(CultureInfo.InvariantCulture, "C{0} {1} {2} {3} {4} {3} ", cx - rx, cy - oy, cx - ox, cy - ry, cx);
            sb.AppendFormat(CultureInfo.InvariantCulture, "C{0} {1} {2} {3} {2} {4} ", cx + ox, cy - ry, cx + rx, cy - oy, cy);
            sb.AppendFormat(CultureInfo.InvariantCulture, "C{0} {1} {2} {3} {4} {3} ", cx + rx, cy + oy, cx + ox, cy + ry, cx);
            sb.AppendFormat(CultureInfo.InvariantCulture, "C{0} {1} {2} {3} {2} {4} Z", cx - ox, cy + ry, cx - rx, cy + oy, cy);
            return sb.ToString();
        }

        /// <summary>A rounded rectangle as path data.</summary>
        public static string RoundRect(float x, float y, float w, float h, float r)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) / 2f);
            return string.Format(CultureInfo.InvariantCulture,
                "M{0} {1} H{2} A{4} {4} 0 0 1 {3} {5} V{6} A{4} {4} 0 0 1 {2} {7} H{0} A{4} {4} 0 0 1 {8} {6} V{5} A{4} {4} 0 0 1 {0} {1} Z",
                x + r, y, x + w - r, x + w, r, y + r, y + h - r, y + h, x);
        }

        static Segment[] Parse(string data)
        {
            if (Cache.TryGetValue(data, out var cached)) return cached;
            var result = new List<Segment>();
            int i = 0;
            char command = 'M';
            Vector2 current = Vector2.zero, start = Vector2.zero;
            while (true)
            {
                SkipSeparators(data, ref i);
                if (i >= data.Length) break;
                char c = data[i];
                if (char.IsLetter(c))
                {
                    command = char.ToUpperInvariant(c);
                    i++;
                    if (command == 'Z')
                    {
                        result.Add(new Segment { Op = Op.Close });
                        current = start;
                        continue;
                    }
                }
                switch (command)
                {
                    case 'M':
                        current = start = ReadPoint(data, ref i);
                        result.Add(new Segment { Op = Op.Move, A = current });
                        command = 'L'; // further coordinate pairs are implicit line-tos
                        break;
                    case 'L':
                        current = ReadPoint(data, ref i);
                        result.Add(new Segment { Op = Op.Line, A = current });
                        break;
                    case 'H':
                        current = new Vector2(ReadNumber(data, ref i), current.y);
                        result.Add(new Segment { Op = Op.Line, A = current });
                        break;
                    case 'V':
                        current = new Vector2(current.x, ReadNumber(data, ref i));
                        result.Add(new Segment { Op = Op.Line, A = current });
                        break;
                    case 'C':
                    {
                        var c1 = ReadPoint(data, ref i);
                        var c2 = ReadPoint(data, ref i);
                        current = ReadPoint(data, ref i);
                        result.Add(new Segment { Op = Op.Cubic, A = c1, B = c2, C = current });
                        break;
                    }
                    case 'Q':
                    {
                        // Stored as the equivalent cubic: Painter2D fills quadratic curves from the wrong start point.
                        var control = ReadPoint(data, ref i);
                        var end = ReadPoint(data, ref i);
                        var c1 = current + (control - current) * (2f / 3f);
                        var c2 = end + (control - end) * (2f / 3f);
                        result.Add(new Segment { Op = Op.Cubic, A = c1, B = c2, C = end });
                        current = end;
                        break;
                    }
                    case 'A':
                    {
                        float rx = ReadNumber(data, ref i), ry = ReadNumber(data, ref i);
                        ReadNumber(data, ref i); // x-axis rotation: irrelevant for circular arcs
                        bool large = ReadNumber(data, ref i) != 0;
                        bool sweep = ReadNumber(data, ref i) != 0;
                        var end = ReadPoint(data, ref i);
                        result.Add(ArcSegment(current, end, (rx + ry) / 2f, large, sweep));
                        current = end;
                        break;
                    }
                    default:
                        throw new FormatException($"Unsupported SVG path command '{command}' in \"{data}\"");
                }
            }
            var segments = result.ToArray();
            Cache[data] = segments;
            return segments;
        }

        /// <summary>Converts an SVG endpoint arc to a center arc (SVG spec, appendix F.6, with rx = ry and no rotation).</summary>
        static Segment ArcSegment(Vector2 from, Vector2 to, float r, bool large, bool sweep)
        {
            if ((to - from).sqrMagnitude < 1e-8f || r < 1e-4f) return new Segment { Op = Op.Line, A = to };
            var mid = (from + to) * 0.5f;
            var half = (from - to) * 0.5f;
            float d2 = half.sqrMagnitude;
            float r2 = r * r;
            if (d2 > r2)
            {
                r = Mathf.Sqrt(d2);
                r2 = d2;
            }
            float coefficient = Mathf.Sqrt(Mathf.Max(0f, (r2 - d2) / d2));
            float sign = large == sweep ? -1f : 1f;
            var center = mid + sign * coefficient * new Vector2(half.y, -half.x);
            return new Segment
            {
                Op = Op.Arc,
                A = center,
                Radius = r,
                From = Mathf.Atan2(from.y - center.y, from.x - center.x),
                To = Mathf.Atan2(to.y - center.y, to.x - center.x),
                Clockwise = sweep,
            };
        }

        static Vector2 ReadPoint(string s, ref int i) => new Vector2(ReadNumber(s, ref i), ReadNumber(s, ref i));

        static float ReadNumber(string s, ref int i)
        {
            SkipSeparators(s, ref i);
            int begin = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                i++;
                if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
                while (i < s.Length && char.IsDigit(s[i])) i++;
            }
            if (i == begin) throw new FormatException($"Expected a number at {begin} in \"{s}\"");
            return float.Parse(s.Substring(begin, i - begin), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static void SkipSeparators(string s, ref int i)
        {
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
        }
    }
}
