namespace RxSample
{
    public enum AsyncMethod
    {
        Synchronous,
        NoAwait,
        OnWorkerThread,
        OnScheduler,
        OnSchedulerHandleErrors,
        OnSchedulerNoErrorHandling
    }
}
