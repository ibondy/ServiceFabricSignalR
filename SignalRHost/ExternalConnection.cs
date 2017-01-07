namespace SignalRHost
{
    #region Using

    using System;
    using System.Threading.Tasks;
    using EchoActor.Interfaces;
    using EchoService;
    using Microsoft.AspNet.SignalR;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using ScaleOut;

    #endregion

    public class ExternalConnection : PersistentConnection
    {
        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            var so = ServiceProxy.Create<IScaleOut>(new Uri(SignalRHostConfiguration.SFScaleoutConnectionString), new ServicePartitionKey(0));
            await so.SetConnectionAsync(connectionId, Environment.MachineName).ConfigureAwait(false);
        }

        protected override async Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            var so = ServiceProxy.Create<IScaleOut>(new Uri(SignalRHostConfiguration.SFScaleoutConnectionString), new ServicePartitionKey(0));
            await so.RemoveConnectionAsync(connectionId).ConfigureAwait(false);
        }

        /// <summary>Called when data is received from a connection.</summary>
        /// <param name="request">The <see cref="T:Microsoft.AspNet.SignalR.IRequest" /> for the current connection.</param>
        /// <param name="connectionId">The id of the connection sending the data.</param>
        /// <param name="data">The payload sent to the connection.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that completes when the receive operation is complete.</returns>
        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            if (SignalRHostConfiguration.UseActors)
            {
                var myActor = ActorProxy.Create<IEchoActor>(new ActorId(connectionId), new Uri("fabric:/EchoTestActor/EchoActorService"));
                await myActor.Echo(Environment.MachineName, connectionId, data);
            }
            else
            {
                var proxy = ServiceProxy.Create<IEchoService>(new Uri("fabric:/EchoTestService/EchoService"), new ServicePartitionKey());
                await proxy.Echo(Environment.MachineName, connectionId, data).ConfigureAwait(false);
            }
        }
    }
}