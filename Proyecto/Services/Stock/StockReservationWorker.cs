using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Proyecto.Services.Stock;

public class StockReservationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReservationWorker> _logger;

    public StockReservationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<StockReservationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "StockReservationWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var service =
                    scope.ServiceProvider
                        .GetRequiredService<StockReservationService>();

                await service.LiberarReservasExpiradasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error liberando reservas expiradas.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }
}
