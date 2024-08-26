namespace Proximate;

public abstract class Service 
{
    protected Service() {}
}

public static class Services
{
    
    private static Dictionary<int, Service> services = new();

    private static int GetHash(Type serviceType) => serviceType?.Name.ToLower().GetHashCode() ?? 0;

    public static T Get<T>() where T : Service => Get(typeof(T)) as T ?? throw new Exception("Invalid Service");

    private static Service Get(Type serviceType)
    {
        if (serviceType.IsSubclassOf(typeof(Service)) == false)
        {
            throw new Exception($"Invalid service type (not a service): {serviceType?.Name}");
        }

        if (serviceType.IsAbstract)
        {
            throw new Exception($"Invalid service type (abstract): {serviceType?.Name}");
        }
        
        var hash = GetHash(serviceType);
        if (hash == 0)
        {
            throw new Exception($"Invalid service type (hash zero): {serviceType?.Name}");
        }

        if (services.ContainsKey(hash) == false)
        {
            var newService = Activator.CreateInstance(serviceType) as Service;
            if (newService == null)
            {
                throw new Exception($"Couldn't create {serviceType.Name} service");
            }
            services[hash] = newService;
        }
        return services[hash]!;
    }
}