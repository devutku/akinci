using System.Net;
using System.Net.Sockets;

namespace Akinci.Infrastructure.Resilience;

/// <summary>
/// Geçici ağ kopmaları, zaman aşımları ve sunucu 5xx/429 hataları için üstel geri çekilme (exponential backoff) retry politikası.
/// </summary>
public static class RetryPolicy
{
    private static readonly Random Jitter = new();

    public static async Task ExecuteAsync(
        Func<int, Task> action,
        int maxRetries = 5,
        TimeSpan? initialDelay = null,
        Action<Exception, int, TimeSpan>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        int attempt = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                attempt++;
                await action(attempt).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt <= maxRetries)
            {
                // Exponential backoff + Jitter: baseDelay * 2^(attempt-1) + random(0-250ms)
                double multiplier = Math.Pow(2, attempt - 1);
                var backoffMs = (int)(delay.TotalMilliseconds * multiplier) + Jitter.Next(0, 250);
                var currentWait = TimeSpan.FromMilliseconds(backoffMs);

                onRetry?.Invoke(ex, attempt, currentWait);

                await Task.Delay(currentWait, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static bool IsTransientException(Exception ex)
    {
        return ex switch
        {
            OperationCanceledException => false, // Kullanıcı iptali retry edilmez

            HttpRequestException httpEx => httpEx.StatusCode switch
            {
                HttpStatusCode.RequestTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                _ => httpEx.InnerException is SocketException
            },

            SocketException or IOException or TimeoutException => true,

            _ => ex.InnerException != null && IsTransientException(ex.InnerException)
        };
    }
}
