using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SignalRHost
{
    /// <summary>
    /// Handles messages from Service Fabric and relays to SignalR clients
    /// </summary>
    public class InternalConnection : PersistentConnection
    {
       /// <summary>
        /// Receives message from SF and sends to SignalR client(s) 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="connectionId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            var message = JsonConvert.DeserializeObject<ScaleOut.ScaleOutMessage>(data);
            if(string.IsNullOrEmpty(message.ConnectionId))
            {
                await GlobalHost.ConnectionManager.GetConnectionContext<ExternalConnection>().Connection.Broadcast(data).ConfigureAwait(false);
            }
            else
            {
               await GlobalHost.ConnectionManager.GetConnectionContext<ExternalConnection>().Connection.Send(connectionId, message.Payload).ConfigureAwait(false);
            }
        }
    }
}
