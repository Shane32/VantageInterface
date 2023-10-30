using GraphQL.Execution;
using VantageInterface;
using VantageServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<IEnumerable<HouseConfiguration>>(builder.Configuration.GetSection("Houses"));
builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddAutoSchema<Query>(s => s.WithMutation<Mutation>().WithSubscription<Subscription>())
    .AddExecutionStrategy<SerialExecutionStrategy>(GraphQLParser.AST.OperationType.Query)
    .AddScopedSubscriptionExecutionStrategy());
builder.Services.AddSingleton<VConnectionManager>();

var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLPlayground("/", new GraphQL.Server.Ui.Playground.PlaygroundOptions { SchemaPollingEnabled = false });

app.Run();
