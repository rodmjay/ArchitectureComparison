public class Query
{
    [GraphQLDescription("Returns a simple greeting message.")]
    public string GetGreeting() => "Hello from GraphQL!";
}