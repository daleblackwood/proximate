    namespace Proximate;

    public class RegisteredSet<T> 
    {
        private Dictionary<int, T> items = new();

        private static int Hash(string key) => key?.ToLowerInvariant()?.GetHashCode() ?? 0;

        public T Register(string key, T item)
        {
            var hash = Hash(key);
            if (items.ContainsKey(hash))
            {
                throw new Exception("Recorder is already set.");
            }
            items[hash] = item;
            return item;
        }

        public T? TryGet(string key)
        {
            var hash = Hash(key);
            if (items.ContainsKey(hash))
            {
                return items[hash];
            }
            return default;
        }

        public T Get(string key) => TryGet(key) ?? throw new Exception($"No {typeof(T).Name} registered.");

        public T GetOrCreate(string key, Func<T> factory)
        {
            var result = TryGet(key);
            if (result == null)
            {
                var instance = factory();
                result = Register(key, instance);
            }
            return result!;
        }

        public void Deregister(T item)
        {
            if (item == null)
            {
                return;
            }
            var hashes = items.Where(x => x.Value != null && x.Value.Equals(item)).Select(x => x.Key);
            foreach (var hash in hashes)
            {
                Deregister(hash);
            }
        }

        public void Deregister(string key) => Deregister(Hash(key));

        private void Deregister(int hash)
        {
            items.Remove(hash);
        }

        public T[] GetAll() => items.Values.ToArray();

    }