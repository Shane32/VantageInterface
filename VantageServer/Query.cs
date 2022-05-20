using GraphQL;

namespace VantageServer
{
    public class Query
    {
        public static House House([Id] int houseIdentifier) { }
    }
}
