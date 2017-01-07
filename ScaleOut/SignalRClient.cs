namespace ScaleOut
{
    #region Using

    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR.Client;

    #endregion

    /// <summary>
    ///     Connects to SignalR hosts
    /// </summary>
    public class SignalRClient
    {
        #region

        private readonly string _endpoint;
        private Connection _connection;

        #endregion

        public SignalRClient(string hostId, string endpoint)
        {
            HostId = hostId;
            _endpoint = endpoint;
        }

        #region

        public string HostId { get; }

        #endregion

        public async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(_endpoint))
                throw new InvalidOperationException("Endpoint missing");
            if (_connection != null)
                throw new InvalidStateException("Connection already exists");

            await OpenConnection();
        }

        private async Task OpenConnection()
        {
            try
            {
                _connection = new Connection(_endpoint)
                {
                    TraceLevel = TraceLevels.All,
                    TraceWriter = Console.Out,
                    Protocol = new Version(1, 5)
                };
                _connection.Received += _connection_Received;
                await _connection.Start();
                var x = 0;
                while (_connection.State != ConnectionState.Connected)
                {
                    await Task.Delay(1000);
                    x++;
                    if (x > 20)
                        break;
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
            }

            ServiceEventSource.Current.Message(_connection.State == ConnectionState.Connected
                ? $"Successfully connected Scaleout client to SignalR host at {_endpoint}"
                : "Unable to connect to SignalR host");
        }

        private void _connection_Received(string obj)
        {
            throw new NotImplementedException("Outbound traffic only!");
        }

        public async Task SendAsync(string connectionId, string message)
        {
            if (_connection.State == ConnectionState.Connected)
                await _connection.Send(new ScaleOutMessage {ConnectionId = connectionId, Payload = message}).ConfigureAwait(false);
        }

        public async Task SendAllAsync(string message)
        {
            await _connection.Send(new ScaleOutMessage {Payload = message}).ConfigureAwait(false);
        }
    }
}