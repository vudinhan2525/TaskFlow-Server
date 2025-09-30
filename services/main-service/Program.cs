using MainService.Infras;


// using MainService.Presentation.Services;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Register Services using Extensions
builder.Services.AddProjectServices();    // Register Use Cases & Repositories
builder.Services.AddGrpcServices();       // Register gRPC Services
builder.Services.AddValidationServices(); // Register Validators
builder.Services.AddAutoMapper(typeof(Program));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSerilogRequestLogging();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var kestrelUrl = builder.Configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
logger.LogInformation("🚀 GRPC server starting on {Addresses}", kestrelUrl);

app.Services.GetRequiredService<MongoDbService>();

// Configure Middleware
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

// Map GRPC Services
app.MapGrpcService<ProjectController>();
app.MapGrpcService<SprintController>();
// app.MapGrpcService<CommentServiceImpl>();
// app.MapGrpcService<UserServiceImpl>();
app.MapGrpcService<UserController>();
app.MapGrpcService<IssueController>();
app.MapGrpcService<ProjectMemberController>();
app.MapGrpcService<CommentController>();
app.MapGrpcService<ProjectTeamController>();

if (app.Environment.IsDevelopment())
{
  app.MapGrpcReflectionService();
}

// Default Route
app.MapGet("/", () => "This is a gRPC service. Use a gRPC client to communicate.");

app.Run();
