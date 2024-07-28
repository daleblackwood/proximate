namespace Proximate;

public class ProxyManager
{
    private static ProxyManager? inst;
    public static ProxyManager Inst
    {
        get
        {
            if (inst == null)
            {
                inst = new ProxyManager();
            }
            return inst;
        }
    }

    private List<IProxy> proxies = new();

    private ProxyManager() {}

    public IProxy Register(IProxy proxy)
    {
        if (proxies.Any(x => x.Id == proxy.Id))
        {
            throw new Exception($"Proxy for {proxy.Id} exists.");
        }
        proxies.Add(proxy);
        return proxy;
    }

    public void TryRegister(IProxy proxy)
    {
        try 
        {
            Register(proxy);
        } 
        catch {}
    }

    public void Deregister(IProxy proxy)
    {
        if (proxies.Any(x => x.Id == proxy.Id))
        {
            proxies.Remove(proxy);
        }
    }

    public IProxy? Get(string id)
    {
        return proxies.Find(x => x.Id == id);
    }

    public IProxy Require(string id)
    {
        var result = proxies.Find(x => x.Id == id);
        if (result == null)
        {
            throw new Exception($"No proxy by id '{id}'");
        }
        return result;
    }

    public IProxy[] GetAll()
    {
        return proxies.ToArray();
    }
}