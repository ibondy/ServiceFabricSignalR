namespace ScaleOut
{
    #region Using

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    #endregion

    /// <summary>
    ///     SignalR scaleout using Reliable Dictionary
    /// </summary>
    public interface IScaleOut : IService
    {
        Task SetConnectionAsync(string key, string value);
        Task<string> GetConnectionAsync(string key);
        Task RemoveConnectionAsync(string value);
        Task<long> GetConnectionCount();

        Task RegisterSignalRHost(string hostName, string endpoint);

        Task UnregisterSignalRHost(string hostName);
    }

    /// <summary>
    ///     Handles all messages from Sevice Fabric destined for SignalR clients
    /// </summary>
    public interface IMessageRouter : IService
    {
        Task SendAsync(string hostId, string connectionId, string message);
        Task BroadcastAsync(string message);
    }

    /// <summary>
    ///     Handles SignalR scaleout using Reliable Dictionary
    ///     An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ScaleOut : StatefulService, IScaleOut, IMessageRouter
    {
        public ScaleOut(StatefulServiceContext context) : base(context)
        {
            MyClients = new Dictionary<string, SignalRClient>();
        }

        #region

        private IReliableDictionary<string, string> MyHosts { get; set; }

        private IDictionary<string, SignalRClient> MyClients { get; }

        #endregion

        public async Task SendAsync(string hostId, string connectionId, string message)
        {
            await MyClients.First(p => p.Key == hostId).Value.SendAsync(connectionId, message).ConfigureAwait(false);
        }

        public async Task BroadcastAsync(string message)
        {
            foreach (var client in MyClients)
                await client.Value.SendAllAsync(message).ConfigureAwait(false);
        }

        public async Task<string> GetConnectionAsync(string key)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myConnections = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections").ConfigureAwait(false);
                var result = await myConnections.TryGetValueAsync(tx, key).ConfigureAwait(false);
                return result.Value;
            }
        }

        public async Task<long> GetConnectionCount()
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myConnections = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections").ConfigureAwait(false);
                var result = await myConnections.GetCountAsync(tx).ConfigureAwait(false);
                return result;
            }
        }

        //public async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<string>> GetMessages(int batchCount)
        //{
        //    using (var tx = StateManager.CreateTransaction())
        //    {
        //        var myQueue = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableQueue<string>>("myQueue").ConfigureAwait(false);
        //        var count = await myQueue.GetCountAsync(tx).ConfigureAwait(false);
        //        var mylist = new System.Collections.Generic.List<string>();
        //        for (var i = batchCount - 1; i >= 0; i--)
        //        {
        //            var msg = await myQueue.TryDequeueAsync(tx).ConfigureAwait(false);
        //            if (string.IsNullOrEmpty(msg.Value))
        //                break;
        //            mylist.Add(msg.Value);
        //        }

        //        await tx.CommitAsync().ConfigureAwait(false);
        //        return mylist.ToArray();
        //    }
        //}

        public async Task RemoveConnectionAsync(string key)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myConnections = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections").ConfigureAwait(false);
                await myConnections.TryRemoveAsync(tx, key).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
            }

            using (var tx = StateManager.CreateTransaction())
            {
                var myConnections = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections").ConfigureAwait(false);
                Debug.WriteLine(await myConnections.GetCountAsync(tx));
                await tx.CommitAsync().ConfigureAwait(false);
            }
        }

        public async Task SetConnectionAsync(string key, string value)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myConnections = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections").ConfigureAwait(false);
                await myConnections.AddOrUpdateAsync(tx, key, value, (k, v) => value).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
            }

            using (var tx = StateManager.CreateTransaction())
            {
                var myConnections = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections");
                Debug.WriteLine(await myConnections.GetCountAsync(tx));
                await tx.CommitAsync().ConfigureAwait(false);
            }
        }

        public async Task RegisterSignalRHost(string hostId, string endpoint)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                if (MyHosts == null)
                    MyHosts = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myHosts").ConfigureAwait(false);

                await MyHosts.AddOrUpdateAsync(tx, hostId, endpoint, (k, v) => endpoint).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                if (MyClients.ContainsKey(hostId))
                    MyClients.Remove(hostId);

                var client = new SignalRClient(hostId, endpoint);
                await client.ConnectAsync().ConfigureAwait(false);
                MyClients.Add(hostId, client);
            }
        }

        public async Task UnregisterSignalRHost(string hostName)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                await MyHosts.TryRemoveAsync(tx, hostName).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle
        ///     client or user requests.
        /// </summary>
        /// <remarks>
        ///     For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(this.CreateServiceRemotingListener)
            };
        }

        /// <summary>
        ///     This is the main entry point for your service replica.
        ///     This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myConnections").ConfigureAwait(false);
            MyHosts = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("myHosts").ConfigureAwait(false);
            MyHosts.DictionaryChanged += MyHosts_DictionaryChanged;

            //load existing host clients and connect
            using (var tx = StateManager.CreateTransaction())
            {
                var e = await MyHosts.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                using (var hosts = e.GetAsyncEnumerator())
                {
                    while (await hosts.MoveNextAsync(cancellationToken))
                    {
                        var host = new SignalRClient(hosts.Current.Key, hosts.Current.Value);
                        await host.ConnectAsync().ConfigureAwait(false);
                        MyClients.Add(hosts.Current.Key, host);
                        ServiceEventSource.Current.Message($"Registered and connected to host {host.HostId}");
                    }
                }
            }
        }

        private void MyHosts_DictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<string, string> e)
        {
            //TODO update hosts
        }
    }
}