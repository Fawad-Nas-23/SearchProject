namespace SearchLogic.Metrics;

public interface IInstrumentation
{
    void RecordCacheHit();
    void RecordCacheMiss();
}