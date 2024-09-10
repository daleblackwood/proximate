namespace Proximate;

public class RecorderService : Service
{
    public static string ToId(string filePathOrPrefix)
    {
        return Path.GetFileNameWithoutExtension(filePathOrPrefix).ToLower().Trim();
    }

    private RegisteredSet<ConsumerRecorder> recorders = new();

    public ConsumerRecorder Register(string key, ConsumerRecorder recorder)
        => recorders.Register(key, recorder);
        
    public ConsumerRecorder? TryGet(string key)
        => recorders.TryGet(key);
    
    public ConsumerRecorder Get(string key)
        => recorders.Get(key);

    public ConsumerRecorder[] GetAll() => recorders.GetAll();

    public ConsumerRecorder GetOrCreate(string key, Func<ConsumerRecorder> factory)
        => recorders.GetOrCreate(key, factory);

    public void Deregister(ConsumerRecorder recorder)
        => recorders.Deregister(recorder);

    public bool Deregister(string key) => recorders.Deregister(key);
}