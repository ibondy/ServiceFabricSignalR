using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using System.Diagnostics;

namespace ScaleOut
{
    /// <summary>
    /// Connects to SignalR hosts
    /// </summary>
    public class SignalRClient
    {
        public string HostId { get; }
        private readonly string _endpoint;
        private Connection _connection;
        public SignalRClient(string hostId,string endpoint)
        {
            HostId = hostId;
            _endpoint = endpoint;
        }

        public async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(_endpoint))
            {
                throw new InvalidOperationException("Endpoint missing");
            }
            if (_connection != null)
            {
                throw new InvalidStateException("Connection already exists");
            }

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
                    { break; }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Debug.WriteLine(_connection.State == ConnectionState.Connected ? $"Successfully connected to SignalR host at {_endpoint}" : "Unable to connect to SignalR host");
        }

        private void _connection_Received(string obj)
        {
            throw new NotImplementedException();
        }

        public async Task SendAsync(string connectionId, string message)
        {
          if(_connection.State == ConnectionState.Connected)
            {
                await _connection.Send(new ScaleOutMessage { ConnectionId = connectionId, Payload = message }).ConfigureAwait(false);
            }  
        }

        public async Task SendAllAsync(string message)
        {
            await _connection.Send(new ScaleOutMessage {Payload = message }).ConfigureAwait(false);
        }
    }
}
