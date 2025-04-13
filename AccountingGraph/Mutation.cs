public class Mutation
{
    [GraphQLDescription("A simple test mutation that returns a greeting.")]
    public string SayHello(string name) => $"Hello, {name}!";
}