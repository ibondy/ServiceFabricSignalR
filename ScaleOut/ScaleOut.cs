namespace ScaleOut
{
    using Enumerable = System.Linq.Enumerable;
    using ServiceRemotingExtensions = Microsoft.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingExtensions;

    /// <summary>
    ///     SignalR scaleout using Reliable Dictionary
    /// </summary>
    public interface IScaleOut : Microsoft.ServiceFabric.Services.Remoting.IService
    {
        System.Threading.Tasks.Task SetConnectionAsync(string key, string value);
        System.Threading.Tasks.Task<string> GetConnectionAsync(string key);
        System.Threading.Tasks.Task RemoveConnectionAsync(string value);
        System.Threading.Tasks.Task<long> GetConnectionCount();

        System.Threading.Tasks.Task RegisterSignalRHost(string hostName, string endpoint);

        System.Threading.Tasks.Task UnregisterSignalRHost(string hostName);

        /// <summary>
        ///     Pulls messages from Scaleout
        /// </summary>
        /// <param name="batchCount"></param>
        /// <returns></returns>
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<string>> GetMessages(int batchCount);
    }

    /// <summary>
    ///     Handles all messages from Sevice Fabric destined for SignalR clients
    /// </summary>
    public interface IMessageRouter : Microsoft.ServiceFabric.Services.Remoting.IService
    {
        System.Threading.Tasks.Task SendAsync(string hostId, string connectionId, string message);
        System.Threading.Tasks.Task BroadcastAsync(string message);
    }

    /// <summary>
    ///     Handles SignalR scaleout using Reliable Dictionary
    ///     An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ScaleOut : Microsoft.ServiceFabric.Services.Runtime.StatefulService, IScaleOut, IMessageRouter
    {
        #region

        private static readonly string _allowedCharacters = "abcdefghjklmnpqrstvxz0123456789";

        private readonly System.Collections.Generic.List<string> _myQueueList = new System.Collections.Generic.List<string>();

        #endregion

        public ScaleOut(System.Fabric.StatefulServiceContext context) : base(context)
        {
            MyClients = new System.Collections.Generic.Dictionary<string, SignalRClient>();
        }

        #region

        private Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string> MyHosts { get; set; }

        private System.Collections.Generic.IDictionary<string, SignalRClient> MyClients { get; }

        #endregion

        public async System.Threading.Tasks.Task SendAsync(string hostId, string connectionId, string message)
        {
            await Enumerable.First(MyClients, p => p.Key == hostId).Value.SendAsync(connectionId, message).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task BroadcastAsync(string message)
        {
            foreach (var client in MyClients)
                await client.Value.SendAllAsync(message).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<string> GetConnectionAsync(string key)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary").ConfigureAwait(false);
                var result = await myDictionary.TryGetValueAsync(tx, key).ConfigureAwait(false);
                return result.Value;
            }
        }

        public async System.Threading.Tasks.Task<long> GetConnectionCount()
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary").ConfigureAwait(false);
                var result = await myDictionary.GetCountAsync(tx).ConfigureAwait(false);
                return result;
            }
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<string>> GetMessages(int batchCount)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myQueue = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableQueue<string>>("myQueue").ConfigureAwait(false);
                var count = await myQueue.GetCountAsync(tx).ConfigureAwait(false);
                var mylist = new System.Collections.Generic.List<string>();
                for (var i = batchCount - 1; i >= 0; i--)
                {
                    var msg = await myQueue.TryDequeueAsync(tx).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(msg.Value))
                        break;
                    mylist.Add(msg.Value);
                }

                await tx.CommitAsync().ConfigureAwait(false);
                return mylist.ToArray();
            }
        }

        public async System.Threading.Tasks.Task RemoveConnectionAsync(string key)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary").ConfigureAwait(false);
                await myDictionary.TryRemoveAsync(tx, key).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
            }

            using (var tx = StateManager.CreateTransaction())
            {
                var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary").ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine(await myDictionary.GetCountAsync(tx));
                await tx.CommitAsync().ConfigureAwait(false);
            }
        }

        public async System.Threading.Tasks.Task SetConnectionAsync(string key, string value)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary").ConfigureAwait(false);
                await myDictionary.AddOrUpdateAsync(tx, key, value, (k, v) => value).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);
            }

            using (var tx = StateManager.CreateTransaction())
            {
                var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary");
                System.Diagnostics.Debug.WriteLine(await myDictionary.GetCountAsync(tx));
                await tx.CommitAsync().ConfigureAwait(false);
            }
        }

        public async System.Threading.Tasks.Task RegisterSignalRHost(string hostId, string endpoint)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                if (MyHosts == null)
                    MyHosts = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myHosts").ConfigureAwait(false);

                await MyHosts.AddOrUpdateAsync(tx, hostId, endpoint, (k, v) => endpoint).ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                if (MyClients.ContainsKey(hostId))
                    MyClients.Remove(hostId);

                var client = new SignalRClient(hostId, endpoint);
                await client.ConnectAsync().ConfigureAwait(false);
                MyClients.Add(hostId, client);
            }
        }

        public async System.Threading.Tasks.Task UnregisterSignalRHost(string hostName)
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
        protected override System.Collections.Generic.IEnumerable<Microsoft.ServiceFabric.Services.Communication.Runtime.ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new Microsoft.ServiceFabric.Services.Communication.Runtime.ServiceReplicaListener(
                    context => ServiceRemotingExtensions.CreateServiceRemotingListener(this, context), "", false)
            };
        }

        /// <summary>
        ///     This is the main entry point for your service replica.
        ///     This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async System.Threading.Tasks.Task RunAsync(System.Threading.CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myDictionary").ConfigureAwait(false);
            var myQueue = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableQueue<string>>("myQueue").ConfigureAwait(false);
            MyHosts = await StateManager.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, string>>("myHosts").ConfigureAwait(false);
            MyHosts.DictionaryChanged += MyHosts_DictionaryChanged;

            //TODO load clients and connect

            //Generate random queue values

            var messages = string.Empty;
            var length = 105;
            using (var tx = StateManager.CreateTransaction())
            {
                for (var i = length - 1; i >= 0; i--)
                {
                    var item = GenerateString(8);
                    await myQueue.EnqueueAsync(tx, item).ConfigureAwait(false);
                    _myQueueList.Add(item);
                }
                await tx.CommitAsync();
            }
        }

        private string GenerateString(int numberOfCharacters)
        {
            const int from = 0;
            var to = _allowedCharacters.Length;
            var r = new System.Random(System.DateTime.Now.Millisecond);

            var qs = new System.Text.StringBuilder();
            for (var i = 0; i < numberOfCharacters; i++)
                qs.Append(_allowedCharacters.Substring(r.Next(from, to), 1));
            return qs.ToString();
        }

        private void MyHosts_DictionaryChanged(object sender, Microsoft.ServiceFabric.Data.Notifications.NotifyDictionaryChangedEventArgs<string, string> e)
        {
            //TODO update hosts
        }
    }
}