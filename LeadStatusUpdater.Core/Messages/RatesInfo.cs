namespace LeadStatusUpdater.Core.Messages;

public class RatesInfo
{
    public DateTime Date { get; set; }
    public Dictionary<string, decimal> Rates { get; set; }
}
