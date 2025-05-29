using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MainService.Infras.Entities;

namespace MainService.Infras;
public class MongoDbService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MongoDbService> _logger;
    private IMongoDatabase? _database;
    private IMongoClient? _mongoClient;
    public IMongoClient MongoClient => _mongoClient ?? throw new InvalidOperationException("MongoClient not initialized. Call Initiate() first.");
    public MongoDbService(IConfiguration configuration, ILogger<MongoDbService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        Initiate();
    }

    public IMongoDatabase Database => _database ?? throw new InvalidOperationException("Database not initialized. Call Initiate() first.");

    public void Initiate()
    {
        try
        {
            RegisterSerializers();

            var connectionStr = _configuration.GetConnectionString("MongoDb");
            var mongoUrl = MongoUrl.Create(connectionStr);
            _mongoClient = new MongoClient(mongoUrl);

            var dbName = mongoUrl.DatabaseName ?? "taskflow";
            _database = _mongoClient.GetDatabase(dbName);

            _logger.LogInformation("MongoDB connection successful!");
        }
        catch (Exception ex)
        {
            _logger.LogError($"MongoDB connection failed: {ex.Message}");
        }
    }

    private void RegisterSerializers()
    {
        try
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Issue)))
            {
                BsonClassMap.RegisterClassMap<Issue>(cm =>
                {
                    cm.AutoMap();
                    cm.GetMemberMap(c => c.Description)
                        .SetSerializer(new DescriptionSerializer());
                });
            }
            _logger.LogInformation("MongoDB serializers registered successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError($"MongoDB serializer registration failed: {ex.Message}");
        }
    }

}
