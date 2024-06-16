using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Responses;

public class MonthlyDepositWithdrawDifferenceResponse
{
    public List<LeadDto> Leads { get; set; }
    public decimal DifferenceThreshold { get; set; } = 13000; 
    public string Currency { get; set; } = "RUB";
}