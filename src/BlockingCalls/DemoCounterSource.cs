using System.Diagnostics.Tracing;

namespace BlockingCalls
{
    [EventSource(Name = "Demo")]
    public class DemoCounterSource : EventSource
    {
        public static DemoCounterSource Log = new DemoCounterSource();

        private EventCounter sqlResponseTimeCounter;

        private DemoCounterSource() : base()
        {
            sqlResponseTimeCounter = new EventCounter("sql-response-time", this)
            {
                DisplayName = "SQL response time (ms)",
                DisplayUnits = "ms"
            };
        }

        public void RecordResponseTime(double ms)
        {
            sqlResponseTimeCounter.WriteMetric(ms);
        }
    }
}
