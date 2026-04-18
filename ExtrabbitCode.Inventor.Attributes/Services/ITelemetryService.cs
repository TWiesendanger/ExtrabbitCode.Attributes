using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public interface ITelemetryService
{
    bool Enabled { get; set; }
    void TrackEvent(string eventName, Dictionary<string, object>? properties = null);
    Task FlushAsync();
}