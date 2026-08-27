using System;

namespace TimeDebt.Core
{
    /// <summary>
    /// One resource with three meanings: remaining lifetime, health and money.
    /// The class is independent from Unity's frame loop so the economy can be tested in isolation.
    /// </summary>
    [Serializable]
    public sealed class TimeBalance
    {
        private const double Epsilon = 0.000001d;

        private double seconds;

        public TimeBalance(double initialSeconds)
        {
            seconds = RequireNonNegative(initialSeconds, nameof(initialSeconds));
        }

        public double Seconds => seconds;

        public bool IsExpired => seconds <= Epsilon;

        public void Reset(double newSeconds)
        {
            seconds = RequireNonNegative(newSeconds, nameof(newSeconds));
        }

        public double Add(double amountSeconds)
        {
            amountSeconds = RequireNonNegative(amountSeconds, nameof(amountSeconds));
            seconds += amountSeconds;
            return seconds;
        }

        public double DrainUpTo(double requestedSeconds)
        {
            requestedSeconds = RequireNonNegative(requestedSeconds, nameof(requestedSeconds));
            double drained = Math.Min(seconds, requestedSeconds);
            seconds -= drained;
            return drained;
        }

        public bool TrySpend(double costSeconds, double safetyReserveSeconds = 0d)
        {
            costSeconds = RequireNonNegative(costSeconds, nameof(costSeconds));
            safetyReserveSeconds = RequireNonNegative(safetyReserveSeconds, nameof(safetyReserveSeconds));

            if (seconds - costSeconds < safetyReserveSeconds)
            {
                return false;
            }

            seconds -= costSeconds;
            return true;
        }

        private static double RequireNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Time must be a finite non-negative number.");
            }

            return value;
        }
    }
}
