using ExtrabbitCode.Inventor.Attributes.Addin;
using log4net;
using PostHog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public sealed class PostHogTelemetryService(string apiKey, string distinctId, bool enabled)
    : ITelemetryService, IDisposable
{
    public bool Enabled { get; set; } = enabled;

    private static readonly ILog Logger =
        LogManagerAddin.GetLogger(typeof(PostHogTelemetryService));

    private readonly PostHogClient _client = new(new PostHogOptions
    {
        ProjectApiKey = apiKey,
        HostUrl = new Uri("https://us.i.posthog.com")
    });

    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            _client.Capture(distinctId, eventName, properties);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Telemetry failed: {ex.Message}", ex);
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            await _client.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Flush failed: {ex.Message}", ex);
        }
    }

    public void Dispose() => _client.Dispose();
}