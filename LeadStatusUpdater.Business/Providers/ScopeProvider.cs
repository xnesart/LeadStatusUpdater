using Microsoft.Extensions.DependencyInjection;

namespace LeadStatusUpdater.Business.Providers;

public class ScopeProvider
{
    private readonly IServiceProvider _serviceProvider;

    public ScopeProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }
}