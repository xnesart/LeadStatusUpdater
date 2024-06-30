using System.Text.Json;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Messages;
using Serilog;

namespace LeadStatusUpdater.Core.Data;

public class CurrencyRatesProvider : ICurrencyRatesProvider
{
    private Dictionary<string, decimal> _rates;
    private readonly ILogger _logger = Log.ForContext<CurrencyRatesProvider>();

    public CurrencyRatesProvider()
    {
        LoadRatesFromFile();
    }

    private void LoadRatesFromFile()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rates.json");

        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                _rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json);

                _logger.Information("Rates loaded from file");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error reading rates from file");
                _rates = new Dictionary<string, decimal>(); // инициализируем пустым словарем в случае ошибки
            }
        }
        else
        {
            _logger.Warning("Rates file not found, initializing with empty rates");
            _rates = new Dictionary<string, decimal>();
        }
    }

    private static string ConvertCurrencyEnumToString(Enum currencyNumber)
    {
        return currencyNumber.ToString().ToUpper();
    }

    public decimal ConvertFirstCurrencyToUsd(Enum currencyNumber)
    {
        var currency = ConvertCurrencyEnumToString(currencyNumber);
        if (_rates == null)
        {
            _logger.Error("Error, currency rates not found.");
            throw new ArgumentException("Error, currency rates not found.");
        }

        if (_rates.TryGetValue(currency, out var rateToUsd))
        {
            _logger.Information(
                $"Returning rate {currency} to USD - {rateToUsd}. / Возврат курса {currency} к USD – {rateToUsd}");
            return rateToUsd;
        }

        _logger.Error(
            $"Throwing an error if rate for {currency} to USD not found. / Выдача ошибки, если курс {currency} к USD не найден.");
        throw new ArgumentException($"Rate for {currency} to USD not found. / Курс {currency} к USD не найден.");
    }

    public decimal ConvertUsdToSecondCurrency(Enum currencyNumber)
    {
        var currency = ConvertCurrencyEnumToString(currencyNumber);
        if (_rates.TryGetValue(currency, out var rateToUsd))
        {
            _logger.Information(
                $"Returning rate USD to {currency} - 1/{rateToUsd}. /  Возврат курса USD к {currency} - 1/{rateToUsd}.");
            return 1 / rateToUsd;
        }

        _logger.Error(
            $"Throwing an error if rate for USD to {currency} not found. / Выдача ошибки, если курс USD к {currency} не найден.");
        throw new ArgumentException($"Rate for USD to {currency} not found. / Курс USD к {currency} не найден.");
    }

    public void SetRates(RatesInfo rates)
    {
        _logger.Information("Rates updated at " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss"));
        _rates = rates.Rates;

        _logger.Information($"{_rates} начали преобразовывать курсы {DateTime.Now}");
        var trimmedRates = GetOnlyNeededRates();
        var result = GetFilteredRates(trimmedRates);
        _logger.Information($"{_rates} закончили преобразовывать курсы {DateTime.Now}");

        _rates = result;
        _logger.Information($"{result}");

        // Сериализуем результат в JSON и записываем в файл
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "result.json");

        try
        {
            File.WriteAllText(filePath, json);
            _logger.Information($"Result has been written to {filePath}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error writing result to file");
        }
    }

    private Dictionary<string, decimal> GetOnlyNeededRates()
    {
        _logger.Information("Обрезаем лишние буквы в словаре");

        var newRates = new Dictionary<string, decimal>();

        foreach (var rate in _rates)
        {
            var trimKey = rate.Key.Remove(0, 3);
            if (!newRates.ContainsKey(trimKey))
            {
                newRates.Add(trimKey, rate.Value);
            }
        }

        return newRates;
    }

    private Dictionary<string, decimal> GetFilteredRates(Dictionary<string, decimal> oldDictionary)
    {
        var validKeys = Enum.GetNames(typeof(Currency));

        _logger.Information("Получаем словарь, который содержит только нужные списки валют");
        var filteredDictionary = oldDictionary.Where(rate => validKeys.Contains(rate.Key)).ToDictionary();
        if (!filteredDictionary.ContainsKey("USD"))
        {
            filteredDictionary.Add("USD", 1);
        }

        filteredDictionary["USD"] = 1;

        return filteredDictionary;
    }
}