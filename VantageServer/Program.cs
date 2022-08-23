using GraphQL.Execution;
using VantageServer;
using VantageServer.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<IEnumerable<HouseConfiguration>>(builder.Configuration.GetSection("Houses"));
builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddAutoSchema<Query>(s => s.WithMutation<Mutation>().WithSubscription<Subscription>())
    .AddExecutionStrategy<SerialExecutionStrategy>(GraphQLParser.AST.OperationType.Query)
    .AddScopedSubscriptionExecutionStrategy());

var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLPlayground("/", new GraphQL.Server.Ui.Playground.PlaygroundOptions { SchemaPollingEnabled = false });

app.Run();
