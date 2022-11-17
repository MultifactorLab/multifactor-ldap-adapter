using System.Linq;
using System;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class RandomWaiterConfig
    {
        public int Min { get; }
        public int Max { get; }
        public bool ZeroDelay { get; }

        protected RandomWaiterConfig(int min, int max)
        {
            Min = min;
            Max = max;
            ZeroDelay = min == 0 && min == max;
        }

        public static RandomWaiterConfig Create(string delaySettings)
        {
            if (string.IsNullOrWhiteSpace(delaySettings))
            {
                return new RandomWaiterConfig(0, 0);
            }

            if (int.TryParse(delaySettings, out var delay))
            {
                if (delay < 0) Throw();
                return new RandomWaiterConfig(delay, delay);
            }

            var splitted = delaySettings.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitted.Length != 2) Throw();

            var values = splitted.Select(x => int.TryParse(x, out var d) ? d : -1).ToArray();
            if (values.Any(x => x < 0)) Throw();

            return new RandomWaiterConfig(values[0], values[1]);
        }

        private static void Throw()
        {
            throw new ArgumentException("Incorrect delay configuration");
        }
    }
}
