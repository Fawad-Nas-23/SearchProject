namespace SearchLogic.Metrics;

public interface IInstrumentation
{
    void RecordCacheHit();
    void RecordCacheMiss();
    void RecordSearchDuration(double seconds, string type);
    void Dispose();

}