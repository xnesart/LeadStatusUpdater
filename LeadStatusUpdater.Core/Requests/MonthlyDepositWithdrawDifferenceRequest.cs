using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Requests;

public class MonthlyDepositWithdrawDifferenceRequest
{
    public List<LeadDto> Leads { get; set; }
    public decimal DifferenceThreshold { get; set; } = 13000; 
    public string Currency { get; set; } = "RUB";
}