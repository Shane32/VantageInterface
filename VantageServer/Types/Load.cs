using GraphQL;

namespace VantageServer.Types
{
    public record Load([Id] int Id, float Level);
}
