namespace SignalRHost
{
    #region Using

    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR;
    using Newtonsoft.Json;
    using ScaleOut;

    #endregion

    /// <summary>
    ///     Handles messages from Service Fabric and relays to SignalR clients
    /// </summary>
    public class InternalConnection : PersistentConnection
    {
        /// <summary>
        ///     Receives message from SF and sends to SignalR client(s)
        /// </summary>
        /// <param name="request"></param>
        /// <param name="connectionId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            var message = JsonConvert.DeserializeObject<ScaleOutMessage>(data);
            if (string.IsNullOrEmpty(message.ConnectionId))
                await GlobalHost.ConnectionManager.GetConnectionContext<ExternalConnection>().Connection.Broadcast(message.Payload).ConfigureAwait(false);
            else
                await GlobalHost.ConnectionManager.GetConnectionContext<ExternalConnection>().Connection.Send(message.ConnectionId, message.Payload).ConfigureAwait(false);
        }
    }
}