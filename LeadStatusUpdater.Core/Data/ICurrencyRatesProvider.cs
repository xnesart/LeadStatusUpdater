using LeadStatusUpdater.Core.Messages;

namespace LeadStatusUpdater.Core.Data;

public interface ICurrencyRatesProvider
{
    decimal ConvertFirstCurrencyToUsd(Enum currencyType);
    decimal ConvertUsdToSecondCurrency(Enum currencyType);
    void SetRates(RatesInfo rates);
}