using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Requests;

public class GetLeadsRequest
{
    public List<LeadDto> Leads { get; set; }
    public int TimePeriodInDays { get; set; }
    public int CountOfTransactions { get; set; }
    public int TimePeriodForCalculatingChange { get; set; }
    public decimal Change { get; set; }
}