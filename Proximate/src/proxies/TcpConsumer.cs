namespace Proximate;

using System.Net;
using System.Net.Sockets;
using System.Text;

public class TcpConsumer : IConsumer
{
    public enum TcpConfigKey
    {
        TCP_PORT,
        TCP_MODE
    }

    public enum TcpMode
    {
        ASCII,
        BINARY
    }

    public class TcpConfig : Config<TcpConfigKey> 
    {
        public override void Validate()
        {
            var portStr = Get(TcpConfigKey.TCP_PORT);
            int.TryParse(portStr, out int portInt);
            if (portInt < 80 || portInt > 64000)
            {
                throw new Exception("Invalid PORT: " + (portStr ?? "(none)"));
            }
        }
    }

    public class TcpConsumerBuilder : IConsumerBuilder
    {
        public static TcpMode ParseTcpMode(string? mode)
        {
            if (string.IsNullOrEmpty(mode) == false)
            {
                mode = mode.Trim().ToUpper();
                foreach (var modeValue in Enum.GetValues<TcpMode>())
                {
                    if (Enum.GetName(modeValue) == mode)
                    {
                        return modeValue;
                    }
                }
            }
            return TcpMode.ASCII;
        }

        public bool CanBuild(Config config)
        {
            return config.Has(TcpConfigKey.TCP_PORT);
        }

        public IConsumer Build(Config inConfig)
        {
            var config = new TcpConfig();
            config.Copy(inConfig);
            config.Id = inConfig.Id;
            config.Validate();

            var mode = ParseTcpMode(config.Get(TcpConfigKey.TCP_MODE));
            int.TryParse(config.Get(TcpConfigKey.TCP_PORT), out int port);
            var consumer = new TcpConsumer(config.Id!, port!);
            return consumer;
        }
    }

    public string Id { get; private set; }
    public int Port { get; private set; }
    public TcpMode Mode { get; private set; } = TcpMode.ASCII;

    public DateTime LastUpdateTime { get; private set; } = DateTime.UnixEpoch;

    private TcpListener? listener;
    private CancellationTokenSource? cts;

    private List<ConsumerMessage> pending = new();
    
    public TcpConsumer(string id, int port, TcpMode mode = TcpMode.ASCII)
    {
        Id = id;
        Port = port;
        Mode = mode;
        CreateServer();
    }

    private void CreateServer()
    {
        if (listener != null) 
        {
            throw new InvalidOperationException("Server is already running.");
        }
        cts = new CancellationTokenSource();
        listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Task.Run(() => AcceptClients(cts.Token));
    }

    private async Task AcceptClients(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = await listener!.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client, token));
            }
        }
        catch (Exception) { }
    }

    private async Task HandleClient(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break;

                string messageContent;
                if (Mode == TcpMode.BINARY)
                {
                    messageContent = Convert.ToBase64String(buffer, 0, bytesRead);
                }
                else
                {
                    messageContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }

                var result = new ConsumerMessage
                {
                    time = DateTime.UtcNow,
                    content = messageContent
                };

                LastUpdateTime = result.time;

                pending.Add(result);
            }
        }
    }

    public void StopServer()
    {
        cts?.Cancel();
        listener?.Stop();
        listener = null;
    }

    public Task<ConsumerMessage[]> Read()
    {
        var result = pending.ToArray();
        pending.Clear();
        return Task.FromResult(result);
    }
}
