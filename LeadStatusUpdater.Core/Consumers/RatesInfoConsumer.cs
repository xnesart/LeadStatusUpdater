using LeadStatusUpdater.Core.Data;
using LeadStatusUpdater.Core.Messages;
using MassTransit;
using Serilog;

namespace LeadStatusUpdater.Core.Consumers;

public class RatesInfoConsumer : IConsumer<RatesInfo>
{
    private readonly ICurrencyRatesProvider _currencyRatesProvider;
    private readonly ILogger _logger = Log.ForContext<RatesInfoConsumer>();

    public RatesInfoConsumer(ICurrencyRatesProvider currencyRatesProvider)
    {
        _currencyRatesProvider = currencyRatesProvider;
    }

    public async Task Consume(ConsumeContext<RatesInfo> context)
    {
        _currencyRatesProvider.SetRates(context.Message);
        _logger.Information($"Getting currency rates: {context.Message.Rates} from Rates Provider");
    }
}