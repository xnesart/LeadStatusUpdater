namespace LeadStatusUpdater.Core.Settings;

public class LeadProcessingSettings
{
    public int TransactionThreshold { get; set; }
    public decimal DepositWithdrawDifferenceThreshold { get; set; }
    public int VipBirthdayPeriodDays { get; set; }
}