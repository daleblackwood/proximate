namespace Proximate;

public class ProxyRecorderService : Service
{
    private RegisteredSet<ProxyRecorder> recorders = new();

    public ProxyRecorder Register(string key, ProxyRecorder recorder)
        => recorders.Register(key, recorder);
        
    public ProxyRecorder Get(string key)
        => recorders.Get(key);

    public ProxyRecorder[] GetAll() => recorders.GetAll();

    public ProxyRecorder GetOrCreate(string key, Func<ProxyRecorder> factory)
        => recorders.GetOrCreate(key, factory);

    public void Deregister(ProxyRecorder recorder)
        => recorders.Deregister(recorder);

    public void Deregister(string key) => recorders.Deregister(key);
}