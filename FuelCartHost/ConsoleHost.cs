using System;
using System.Threading.Tasks;
using System.Threading;

// Helper class for hosting a .NET Core console on any platform.
// From the console app, call WaitForShutdown() and the app will remain active until stopped with Ctrl-C
// This is especially helpful when installing a console app as a Linux daemon, but also for directly executing the console app
// Originally found here: https://stackoverflow.com/questions/41454563/how-to-write-a-linux-daemon-with-net-core

static class ConsoleHost
{
    // Block the calling thread until shutdown is triggered via Ctrl+C or SIGTERM.
    public static void WaitForShutdown()
    {
        WaitForShutdownAsync().GetAwaiter().GetResult();
    }

    // Runs an application and block the calling thread until host shutdown.
    public static void Wait()
    {
        WaitAsync().GetAwaiter().GetResult();
    }

    // Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
    public async static Task WaitAsync(CancellationToken token = default(CancellationToken))
    {
        // Wait for the token shutdown if it can be cancelled
        if (token.CanBeCanceled)
        {
            await WaitAsync(token, shutdownMessage: null);
            return;
        }
        // If token cannot be cancelled, attach Ctrl+C and SIGTERN shutdown
        var done = new ManualResetEventSlim(false);
        using (var cts = new CancellationTokenSource())
        {
            AttachCtrlcSigtermShutdown(cts, done, shutdownMessage: "Application is shutting down...");
            await WaitAsync(cts.Token, "Application running. Press Ctrl+C to shut down.");
            done.Set();
        }
    }

    // Returns a Task that completes when shutdown is triggered via the given token, Ctrl+C or SIGTERM.
    public async static Task WaitForShutdownAsync(CancellationToken token = default(CancellationToken))
    {
        var done = new ManualResetEventSlim(false);

        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            AttachCtrlcSigtermShutdown(cts, done, shutdownMessage: string.Empty);
            await WaitForTokenShutdownAsync(cts.Token);
            done.Set();
        }
    }

    private async static Task WaitAsync(CancellationToken token, string shutdownMessage)
    {
        if (!string.IsNullOrEmpty(shutdownMessage))
            Console.WriteLine(shutdownMessage);
        await WaitForTokenShutdownAsync(token);
    }

    private static void AttachCtrlcSigtermShutdown(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
    {
        Action ShutDown = () =>
        {
            if (!cts.IsCancellationRequested)
            {
                if (!string.IsNullOrWhiteSpace(shutdownMessage))
                    Console.WriteLine(shutdownMessage);
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
            resetEvent.Wait();
        };

        AppDomain.CurrentDomain.ProcessExit += delegate { ShutDown(); };
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            ShutDown();
            eventArgs.Cancel = true;
        };
    }

    private async static Task WaitForTokenShutdownAsync(CancellationToken token)
    {
        var waitForStop = new TaskCompletionSource<object>();
        token.Register(obj =>
        {
            var tcs = (TaskCompletionSource<object>)obj;
            tcs.TrySetResult(null);
        }, waitForStop);
        await waitForStop.Task;
    }
}
