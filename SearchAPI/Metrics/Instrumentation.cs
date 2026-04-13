using System.Diagnostics.Metrics;

namespace SearchLogic.Metrics;

public class Instrumentation : IInstrumentation, IDisposable
{
    internal const string MeterName = "SearchAPI.Metrics";
    private readonly Meter _meter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;

    public Instrumentation()
    {
        string? version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();
        _meter = new Meter(MeterName, version);
        _cacheHitCounter = _meter.CreateCounter<long>("searchapi.cache_hits",
            description: "Number of cache hits");
        _cacheMissCounter = _meter.CreateCounter<long>("searchapi.cache_misses",
            description: "Number of cache misses");
    }

    public void RecordCacheHit() => _cacheHitCounter.Add(1);
    public void RecordCacheMiss() => _cacheMissCounter.Add(1);

    public void Dispose() => _meter.Dispose();
}