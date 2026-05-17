using System.Diagnostics.Metrics;

namespace SearchLogic.Metrics;

public class Instrumentation : IInstrumentation, IDisposable
{
    internal const string MeterName = "SearchAPI.Metrics";
    private readonly Meter _meter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;

    // Nyt Histogram til at måle svartid
    private readonly Histogram<double> _searchDuration;

    public Instrumentation()
    {
        string? version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();
        _meter = new Meter(MeterName, version);

        _cacheHitCounter = _meter.CreateCounter<long>("searchapi.cache_hits",
            description: "Number of cache hits");

        _cacheMissCounter = _meter.CreateCounter<long>("searchapi.cache_misses",
            description: "Number of cache misses");

        // Initialisér histogrammet. Vi bruger 'seconds' som standard enhed i Prometheus.
        _searchDuration = _meter.CreateHistogram<double>("searchapi.search_duration_seconds",
            unit: "s",
            description: "Duration of search requests");
    }

    public void RecordCacheHit() => _cacheHitCounter.Add(1);
    public void RecordCacheMiss() => _cacheMissCounter.Add(1);

    // Ny metode til at registrere tid med en label (hit/miss)
    public void RecordSearchDuration(double seconds, string type)
    {
        _searchDuration.Record(seconds, new KeyValuePair<string, object?>("cache_type", type));
    }

    public void Dispose() => _meter.Dispose();
}