using LeadStatusUpdater.Core.Enums;

namespace Messaging.Shared;

public class ConfigurationMessage
{
    public ServiceType ServiceType { get; set; }
    public Dictionary<string, string> Configurations { get; set; }
}