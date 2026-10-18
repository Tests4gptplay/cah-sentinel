namespace CAHSentinel.Core;

/// <summary>Absorb short local-sync read failures without showing a false offline alert.</summary>
public static class ReadRetry
{
    public const int MaxAttempts = 3;
    public static async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> read,
        CancellationToken ct, TimeSpan? retryDelay = null)
    {
        for (var attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return await read(ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception) when (attempt < MaxAttempts && !ct.IsCancellationRequested)
            {
                await Task.Delay(retryDelay ?? TimeSpan.FromSeconds(2), ct);
            }
        }
    }
}
