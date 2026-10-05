using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IndieStudio.Simulation
{
    public enum BreakdownKind { Base, Add, Multiply }

    public readonly struct BreakdownLine
    {
        public readonly string Label;
        public readonly double Amount;
        public readonly BreakdownKind Kind;

        public BreakdownLine(string label, double amount, BreakdownKind kind)
        {
            Label = label;
            Amount = amount;
            Kind = kind;
        }
    }

    /// <summary>
    /// A derived value plus the labeled contributions that produced it. This is the data behind
    /// every "Why?" tooltip (Design/18_UI_UX.md): explainability is built into the math.
    /// </summary>
    public sealed class Breakdown
    {
        readonly List<BreakdownLine> _lines = new List<BreakdownLine>();

        public Breakdown(string baseLabel, double baseValue)
        {
            Value = baseValue;
            _lines.Add(new BreakdownLine(baseLabel, baseValue, BreakdownKind.Base));
        }

        public double Value { get; private set; }
        public IReadOnlyList<BreakdownLine> Lines => _lines;

        public Breakdown Add(string label, double amount)
        {
            Value += amount;
            _lines.Add(new BreakdownLine(label, amount, BreakdownKind.Add));
            return this;
        }

        public Breakdown Multiply(string label, double factor)
        {
            Value *= factor;
            _lines.Add(new BreakdownLine(label, factor, BreakdownKind.Multiply));
            return this;
        }

        /// <summary>Formats the breakdown for a tooltip, e.g. "Base 0.73 / × Burnout 0.92".</summary>
        public string Describe(string valueFormat = "0.##")
        {
            var sb = new StringBuilder();
            foreach (var line in _lines)
            {
                string amount = line.Amount.ToString(valueFormat, CultureInfo.InvariantCulture);
                switch (line.Kind)
                {
                    case BreakdownKind.Base: sb.Append(line.Label).Append(": ").Append(amount); break;
                    case BreakdownKind.Add: sb.Append('\n').Append(line.Amount >= 0 ? "+ " : "- ").Append(line.Label).Append(": ")
                        .Append(System.Math.Abs(line.Amount).ToString(valueFormat, CultureInfo.InvariantCulture)); break;
                    default: sb.Append("\n× ").Append(line.Label).Append(": ").Append(amount); break;
                }
            }
            sb.Append("\n= ").Append(Value.ToString(valueFormat, CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
