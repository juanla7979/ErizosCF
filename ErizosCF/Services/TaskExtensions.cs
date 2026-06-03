public static class TaskExtensions
{
    public static void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                onException?.Invoke(t.Exception);
                // O bien loguear: Console.WriteLine(t.Exception);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
