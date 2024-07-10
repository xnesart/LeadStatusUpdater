using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Responses;
using LeadStatusUpdater.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

public class ProcessingServiceTests
{
    private readonly Mock<IOptionsMonitor<LeadProcessingSettings>> _mockOptionsMonitor;
    private readonly Mock<ILogger<ProcessingService>> _mockLogger;
    private readonly ProcessingService _processingService;

    public ProcessingServiceTests()
    {
        _mockOptionsMonitor = new Mock<IOptionsMonitor<LeadProcessingSettings>>();
        _mockLogger = new Mock<ILogger<ProcessingService>>();

        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(GetDefaultSettings());

        _processingService = new ProcessingService(_mockOptionsMonitor.Object, _mockLogger.Object);
    }

    private LeadProcessingSettings GetDefaultSettings()
    {
        return new LeadProcessingSettings
        {
            BillingPeriodForTransactionsCount = 30,
            TransactionsCount = 5,
            BillingPeriodForDifferenceBetweenDepositAndWithdraw = 30,
            DifferenceBetweenDepositAndWithdraw = 1000,
            BillingPeriodForBirthdays = 7
        };
    }

    [Fact]
    public void SetLeadStatusByTransactions_Returns_Expected_Leads()
    {
        var transactionResponses = GetSampleTransactionResponses();

        // Mock settings to match test conditions
        _mockOptionsMonitor.Setup(x => x.CurrentValue)
            .Returns(new LeadProcessingSettings
            {
                BillingPeriodForTransactionsCount = 30,
                TransactionsCount = 1,
                BillingPeriodForDifferenceBetweenDepositAndWithdraw = 30,
                DifferenceBetweenDepositAndWithdraw = 1000
            });

        var result = _processingService.SetLeadStatusByTransactions(transactionResponses);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, id => id == transactionResponses[0].LeadId);
        Assert.Contains(result, id => id == transactionResponses[1].LeadId);
        Assert.Contains(result, id => id == transactionResponses[2].LeadId);
    }

    [Fact]
    public void SetLeadStatusByTransactions_Returns_Empty_When_No_Valid_Transactions()
    {
        var transactionResponses = new List<TransactionResponse>
        {
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-40), TransactionType = TransactionType.Deposit, AmountInRUB = 5000 },
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-40), TransactionType = TransactionType.Deposit, AmountInRUB = 2000 }
        };

        var result = _processingService.SetLeadStatusByTransactions(transactionResponses);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SetLeadStatusByTransactions_Logs_Information()
    {
        var transactionResponses = new List<TransactionResponse>
        {
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-10), TransactionType = TransactionType.Deposit, AmountInRUB = 5000 }
        };

        _processingService.SetLeadStatusByTransactions(transactionResponses);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("BillingPeriodForTransactionsCount")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetLeadStatusByTransactions_Handles_Empty_TransactionList()
    {
        var result = _processingService.SetLeadStatusByTransactions(new List<TransactionResponse>());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SetLeadStatusByTransactions_Handles_Null_TransactionList()
    {
        var result = _processingService.SetLeadStatusByTransactions(null);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SetLeadStatusByTransactions_Throws_Exception_For_Invalid_Options()
    {
        var invalidOptionsMonitor = new Mock<IOptionsMonitor<LeadProcessingSettings>>();
        invalidOptionsMonitor.Setup(x => x.CurrentValue).Returns((LeadProcessingSettings)null);

        var serviceWithInvalidOptions = new ProcessingService(invalidOptionsMonitor.Object, _mockLogger.Object);

        var transactionResponses = new List<TransactionResponse>
        {
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-10), TransactionType = TransactionType.Deposit, AmountInRUB = 5000 }
        };

        Assert.Throws<NullReferenceException>(() =>
            serviceWithInvalidOptions.SetLeadStatusByTransactions(transactionResponses));
    }

    private List<TransactionResponse> GetSampleTransactionResponses()
    {
        return new List<TransactionResponse>
        {
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-10), TransactionType = TransactionType.Deposit, AmountInRUB = 5000 },
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-10), TransactionType = TransactionType.Deposit, AmountInRUB = 2000 },
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-5), TransactionType = TransactionType.Deposit, AmountInRUB = 2000 },
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-5), TransactionType = TransactionType.Withdraw, AmountInRUB = 500 },
            new TransactionResponse { LeadId = Guid.NewGuid(), Date = DateTime.Now.AddDays(-15), TransactionType = TransactionType.Withdraw, AmountInRUB = 1000 }
        };
    }
}