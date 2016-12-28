namespace SignalRHost
{
    using System;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using ScaleOut;

    public class ExternalConnection : PersistentConnection
    {
        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            var so = ServiceProxy.Create<IScaleOut>(new Uri("fabric:/SFScaleOut/ScaleOut"),
                new ServicePartitionKey());
            await so.SetConnectionAsync(connectionId, FabricRuntime.GetNodeContext().NodeId.ToString()).ConfigureAwait(false);
        }

        protected override async Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            var so = ServiceProxy.Create<IScaleOut>(new Uri("fabric:/SFScaleOut/ScaleOut"),new ServicePartitionKey(0));

            await so.RemoveConnectionAsync(connectionId).ConfigureAwait(false);
        }
    }
}