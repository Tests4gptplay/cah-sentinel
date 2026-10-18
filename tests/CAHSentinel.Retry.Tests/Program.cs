using CAHSentinel.Core;
foreach(var failures in new[] { 0, 1, 2, 3 })
{
    int calls = 0; bool failed = false;
    try
    {
        var value = await ReadRetry.RunAsync<int>(_ => ++calls <= failures
            ? Task.FromException<int>(new InvalidOperationException("synthetic read failure"))
            : Task.FromResult(42), CancellationToken.None, TimeSpan.Zero);
        if(value != 42) throw new Exception("Wrong result");
    }
    catch(InvalidOperationException) { failed = true; }
    if(calls != Math.Min(failures + 1, 3) || failed != (failures == 3)) throw new Exception("Wrong retry threshold");
    Console.WriteLine($"PASS failures={failures} attempts={calls} terminal_error={failed}");
}
using(var cancel = new CancellationTokenSource())
{
    int calls = 0;
    var task = ReadRetry.RunAsync<int>(_ => { calls++; return Task.FromException<int>(new InvalidOperationException()); }, cancel.Token);
    cancel.Cancel();
    try { await task; throw new Exception("Cancellation ignored"); }
    catch(OperationCanceledException) { }
    if(calls != 1) throw new Exception("Retried after cancellation");
    Console.WriteLine("PASS cancel stops pending retry");
}
int resetCalls = 0;
await ReadRetry.RunAsync(_ => Task.FromResult(++resetCalls), CancellationToken.None);
if(resetCalls != 1) throw new Exception("New poll inherited retry count");
Console.WriteLine("PASS success resets retry budget for next poll");
