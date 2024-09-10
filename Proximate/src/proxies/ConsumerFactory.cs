namespace Proximate;

public class ConsumerFactory : Factory<Config, IConsumer>
{
    private static ConsumerFactory? instance = null;
    public static ConsumerFactory Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ConsumerFactory();
            }
            return instance;
        }
    }

    private ConsumerFactory() 
    {
        Add(
            new ApiConsumer.ApiConsumerBuilder(),
            new TcpConsumer.TcpConsumerBuilder()
        );
    }
}