using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidateEvalResults
{
    public class ConfidenceInterval
    {
        public readonly double Percentage;
        public readonly double Lower;
        public readonly double Upper;
        public readonly double Mean;
        public readonly double MarginOfError;

        public ConfidenceInterval(double p, double mean, double marginOfError)
        {
            Percentage = p;
            Lower = mean - marginOfError;
            Upper = mean + marginOfError;
            Mean = mean;
            MarginOfError = marginOfError;
        }

        public ConfidenceInterval(double p, double mean, double stddev, double count)
            : this(p, mean, GetMarginOfError(Z(p), stddev, count))
        {
        }

        public ConfidenceInterval(double p, double mean, double stddev, double count, double z)
            : this(p, mean, GetMarginOfError(z, stddev, count))
        {
        }

        public ConfidenceInterval(double p, IEnumerable<double> percentages)
        {
            double sum = percentages.Sum();
            double count = percentages.Count();
            Mean = sum / count;
            double stddev = Math.Sqrt(percentages.Sum(r => Math.Pow(r - Mean, 2)) / (count - 1)); ;
            MarginOfError = GetMarginOfError(Z(p), stddev, count);
            Percentage = p;
            Lower = Mean - MarginOfError;
            Upper = Mean + MarginOfError;
        }

        public static double Z(double p)
        {
            if (p == 0.99) return 2.58;
            if (p == 0.95) return 1.96;
            if (p == 0.90) return 1.645;

            throw new NotImplementedException();
        }

        public static double GetMarginOfError(double z, double stddev, double count)
        {
            return z * stddev / Math.Sqrt(count);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Lower, Upper);
        }
    }
}
