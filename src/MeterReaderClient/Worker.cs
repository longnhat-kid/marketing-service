using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using MeterReader.Services;

namespace MeterReaderClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly ReadingFactory _readingFactory;
        private MeterReadingService.MeterReadingServiceClient _client;

        public Worker(ILogger<Worker> logger, IConfiguration config, ReadingFactory readingFactory)
        {
            _logger = logger;
            _config = config;
            _readingFactory = readingFactory;
        }

        protected MeterReadingService.MeterReadingServiceClient Client
        {
            get
            {
                if(_client == null)
                {
                    var channel = GrpcChannel.ForAddress(_config.GetValue<string>("Service:ServerUrl"));
                    _client = new MeterReadingService.MeterReadingServiceClient(channel);
                }
                return _client;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var customerId = _config.GetValue<int>("Service:CustomerId");

                var packet = new ReadingPacket()
                {
                    Successful = ReadingStatus.Success,
                    Notes = "Test",
                };

                for (int i = 0; i < 5; i++)
                {
                    var reading = await _readingFactory.Generate(customerId);
                    packet.Readings.Add(reading);
                }

                var result = await Client.AddReadingAsync(packet);
                if(result.Success == ReadingStatus.Success)
                {
                    _logger.LogInformation("Successfully sent");
                }
                else
                {
                    _logger.LogInformation("Failed to sent");
                }

                await Task.Delay(_config.GetValue<int>("Service:DelayInterval"), stoppingToken);
            }
        }
    }
}