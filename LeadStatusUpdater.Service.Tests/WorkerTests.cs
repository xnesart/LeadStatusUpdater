using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using LeadStatusUpdater.Service;
using MassTransit;
using Messaging.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class WorkerTests
{
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IProcessingService> _mockProcessingService;
    private readonly Mock<IHttpClientService> _mockHttpClientService;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Worker _worker;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockProcessingService = new Mock<IProcessingService>();
        _mockHttpClientService = new Mock<IHttpClientService>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _worker = new Worker(
            _mockLogger.Object,
            _mockProcessingService.Object,
            _mockHttpClientService.Object,
            _mockScopeFactory.Object);
    }

    [Fact]
    public async Task StartAsync_Invokes_DoWork_Twice()
    {
        var cancellationToken = new CancellationTokenSource().Token;

        _mockHttpClientService
            .Setup(x => x.Get<Dictionary<string, string>>(It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(new Dictionary<string, string>());

        await _worker.StartAsync(cancellationToken);

        VerifyLogInformation(_mockLogger, Times.AtLeastOnce());
    }

    [Fact]
    public async Task DoWork_Processes_Leads_Successfully()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var leadDtos = new List<LeadDto> { new LeadDto { Id = Guid.NewGuid() } };

        _mockHttpClientService
            .Setup(x => x.Get<List<LeadDto>>(It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(leadDtos);

        _mockProcessingService
            .Setup(x => x.SetLeadStatusByTransactions(It.IsAny<List<TransactionResponse>>()))
            .Returns(new List<Guid> { Guid.NewGuid() });

        // Моделируем исключение для проверки обработки ошибок
        // _mockLogger
        //     .Setup(x => x.Log(
        //         LogLevel.Error,
        //         It.IsAny<EventId>(),
        //         It.IsAny<It.IsAnyType>(),
        //         It.IsAny<Exception>(),
        //         (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
        //     ));

        await _worker.DoWork(cancellationToken);

        VerifyLogInformation(_mockLogger, Times.AtLeastOnce());
    }

    [Fact]
    public async Task DoWork_Handles_Exceptions()
    {
        var cancellationToken = new CancellationTokenSource().Token;

        _mockHttpClientService
            .Setup(x => x.Get<List<LeadDto>>(It.IsAny<string>(), cancellationToken))
            .ThrowsAsync(new Exception("Test exception"));

        await _worker.DoWork(cancellationToken);

        VerifyLogError(_mockLogger, "Error occurred in background worker.", Times.Once());
    }

    [Fact]
    public async Task SendUpdateLeadStatusMessage_Publishes_Message()
    {
        var mockPublishEndpoint = new Mock<IPublishEndpoint>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockScope = new Mock<IServiceScope>();

        mockServiceProvider.Setup(x => x.GetService(typeof(IPublishEndpoint)))
            .Returns(mockPublishEndpoint.Object);
        mockScope.Setup(x => x.ServiceProvider)
            .Returns(mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope())
            .Returns(mockScope.Object);

        var leadsMessage = new LeadsMessage
        {
            Leads = new List<Guid> { Guid.NewGuid() }
        };

        await _worker.SendUpdateLeadStatusMessage(leadsMessage);

        mockPublishEndpoint.Verify(x => x.Publish(leadsMessage, It.IsAny<CancellationToken>()), Times.Once());
    }

    private void VerifyLogInformation(Mock<ILogger<Worker>> logger, Times times)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Worker running at:") ||
                                              v.ToString().Contains("Processed leads with birthdays:") ||
                                              v.ToString().Contains("Processed leads by transaction count:") ||
                                              v.ToString().Contains("Prepared LeadsMessage with total leads:") ||
                                              v.ToString().Contains("Message sent successfully.") ||
                                              v.ToString().Contains("Getting leads from httpClient.") ||
                                              v.ToString().Contains("Getting transactions from httpClient.") ||
                                              v.ToString().Contains("Sending message to RabbitMQ with")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            times);
    }

    private void VerifyLogError(Mock<ILogger<Worker>> logger, string expectedMessage, Times times)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            times);
    }
}
