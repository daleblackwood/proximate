namespace Proximate;

public class ConsumerMessage
{
    public required DateTime time;
    public required string content;
}

public interface IConsumer
{

    string Id { get; }
    DateTime LastUpdateTime { get; }
    DateTime NextUpdateTime { get; }
    Task<ConsumerMessage[]> Read();
}

public interface IConsumerBuilder : IFactoryBuilder<Config, IConsumer>
{

    IConsumer Build() => throw new NotSupportedException("Consumer not supported.");
}