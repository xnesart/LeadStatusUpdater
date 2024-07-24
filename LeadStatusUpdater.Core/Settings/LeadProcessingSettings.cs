namespace LeadStatusUpdater.Core.Settings;

public class LeadProcessingSettings
{
    //правило
    public int BillingPeriodForTransactionsCount { get; set; }
    public int TransactionsCount { get; set; }
    //второе правило
    public int BillingPeriodForDifferenceBetweenDepositAndWithdraw { get; set; }
    public decimal DifferenceBetweenDepositAndWithdraw { get; set; }
    //третье
    public int BillingPeriodForBirthdays { get; set; }
}