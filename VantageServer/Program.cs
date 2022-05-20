using GraphQL;
using GraphQL.AspNetCore3;
using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddAutoSchema<Query>(s => s.WithMutation<Mutation>().WithSubscription<Subscription>()));

var app = builder.Build();

app.UseGraphQL();
app.UseGraphQLPlayground(new GraphQL.Server.Ui.Playground.PlaygroundOptions { SchemaPollingEnabled = false }, "/");

app.Run();
