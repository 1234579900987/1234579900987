using Hylasoft.Opc.Common;
using Opc;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Factory = OpcCom.Factory;
using OpcDa = Opc.Da;

namespace Hylasoft.Opc.Da
{
    /// <summary>
    /// Client Implementation for DA
    /// </summary>
    public class DaClient : IOpcClient
    {
        private readonly URL _url;
        private OpcDa.Server _server;
        private long _sub;

        // default monitor interval in Milliseconds
        private const int DefaultMonitorInterval = 100;

        /// <summary>
        /// Initialize a new Data Access Client
        /// </summary>
        /// <param name="serverUrl">The url of the server to connect to. WARNING: If server URL includes
        /// spaces (ex. "RSLinx OPC Server") then pass the server URL in to the constructor as an Opc.URL object
        /// directly instead.</param>
        public DaClient(Uri serverUrl)
        {
            this._url = new URL(serverUrl.AbsolutePath)
            {
                Scheme = serverUrl.Scheme,
                HostName = serverUrl.Host
            };
        }

        /// <summary>
        /// Initialize a new Data Access Client
        /// </summary>
        /// <param name="serverUrl">The url of the server to connect to</param>
        public DaClient(URL serverUrl)
        {
            this._url = serverUrl;
        }

        /// <summary>
        /// Gets the datatype of an OPC tag
        /// </summary>
        /// <param name="tag">Tag to get datatype of</param>
        /// <returns>System Type</returns>
        public System.Type GetDataType(string tag)
        {
            OpcDa.Item item = new OpcDa.Item { ItemName = tag };
            OpcDa.ItemProperty result;
            try
            {
                OpcDa.ItemPropertyCollection propertyCollection = this._server.GetProperties(new[] { item }, new[] { new OpcDa.PropertyID(1) }, false)[0];
                result = propertyCollection[0];
            }
            catch (NullReferenceException)
            {
                throw new OpcException("Could not find node because server not connected.");
            }
            return result.DataType;
        }

        /// <summary>
        /// OpcDa underlying server object.
        /// </summary>
        protected OpcDa.Server Server
        {
            get
            {
                return this._server;
            }
        }

        #region interface methods

        /// <summary>
        /// Connect the client to the OPC Server
        /// </summary>
        public void Connect()
        {
            if (this.Status == OpcStatus.Connected)
                return;
            this._server = new OpcDa.Server(new Factory(), this._url);
            this._server.Connect();

            this.Status = OpcStatus.Connected;
        }

        /// <summary>
        /// Gets the current status of the OPC Client
        /// </summary>
        public OpcStatus Status { get; private set; }


        /// <summary>
        /// Read a tag
        /// </summary>
        /// <typeparam name="T">The type of tag to read</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        public ReadEvent<T> Read<T>(string tag)
        {
            OpcDa.Item item = new OpcDa.Item { ItemName = tag };
            if (this.Status == OpcStatus.NotConnected)
            {
                throw new OpcException("Server not connected. Cannot read tag.");
            }
            OpcDa.ItemValueResult result = this._server.Read(new[] { item })[0];
            T casted;
            this.TryCastResult(result.Value, out casted);

            ReadEvent<T> readEvent = new ReadEvent<T>();
            readEvent.Value = casted;
            readEvent.SourceTimestamp = result.Timestamp;
            readEvent.ServerTimestamp = result.Timestamp;
            if (result.Quality == OpcDa.Quality.Good) readEvent.Quality = Quality.Good;
            if (result.Quality == OpcDa.Quality.Bad) readEvent.Quality = Quality.Bad;

            return readEvent;
        }

        /// <summary>
        /// Write a value on the specified opc tag
        /// </summary>
        /// <typeparam name="T">The type of tag to write on</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` writes on the tag `bar` on the folder `foo`</param>
        /// <param name="item"></param>
        public void Write<T>(string tag, T item)
        {
            OpcDa.ItemValue itmVal = new OpcDa.ItemValue
            {
                ItemName = tag,
                Value = item
            };
            IdentifiedResult result = this._server.Write(new[] { itmVal })[0];
            CheckResult(result, tag);
        }

        /// <summary>
        /// Casts result of monitoring and reading values
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="casted">The casted result</param>
        /// <typeparam name="T">Type of object to try to cast</typeparam>
        public void TryCastResult<T>(object value, out T casted)
        {
            try
            {
                casted = (T)value;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException(
                  string.Format(
                    "Could not monitor tag. Cast failed for type \"{0}\" on the new value \"{1}\" with type \"{2}\". Make sure tag data type matches.",
                    typeof(T), value, value.GetType()));
            }
        }

        /// <summary>
        /// Monitor the specified tag for changes
        /// </summary>
        /// <typeparam name="T">the type of tag to monitor</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` monitors the tag `bar` on the folder `foo`</param>
        /// <param name="callback">the callback to execute when the value is changed.
        /// The first parameter is a MonitorEvent object which represents the data point, the second is an `unsubscribe` function to unsubscribe the callback</param>
        public void Monitor<T>(string tag, Action<ReadEvent<T>, Action> callback)
        {
            OpcDa.SubscriptionState subItem = new OpcDa.SubscriptionState
            {
                Name = (++this._sub).ToString(CultureInfo.InvariantCulture),
                Active = true,
                UpdateRate = DefaultMonitorInterval
            };
            OpcDa.ISubscription sub = this._server.CreateSubscription(subItem);

            // I have to start a new thread here because unsubscribing
            // the subscription during a datachanged event causes a deadlock
            Action unsubscribe = () => new Thread(o => this._server.CancelSubscription(sub)).Start();

            sub.DataChanged += (handle, requestHandle, values) =>
            {
                T casted;
                this.TryCastResult(values[0].Value, out casted);
                ReadEvent<T> monitorEvent = new ReadEvent<T>();
                monitorEvent.Value = casted;
                monitorEvent.SourceTimestamp = values[0].Timestamp;
                monitorEvent.ServerTimestamp = values[0].Timestamp;
                if (values[0].Quality == OpcDa.Quality.Good) monitorEvent.Quality = Quality.Good;
                if (values[0].Quality == OpcDa.Quality.Bad) monitorEvent.Quality = Quality.Bad;
                callback(monitorEvent, unsubscribe);
            };
            sub.AddItems(new[] { new OpcDa.Item { ItemName = tag } });
            sub.SetEnabled(true);
        }

        /// <summary>
        /// Read a tag asynchronusly
        /// </summary>
        public async Task<ReadEvent<T>> ReadAsync<T>(string tag)
        {
            return await Task.Run(() => this.Read<T>(tag));
        }

        /// <summary>
        /// Write a value on the specified opc tag asynchronously
        /// </summary>
        public async Task WriteAsync<T>(string tag, T item)
        {
            await Task.Run(() => this.Write(tag, item));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this._server?.Dispose();
            this.Status = OpcStatus.NotConnected;

            GC.SuppressFinalize(this);
        }

        #endregion


        private static void CheckResult(IResult result, string tag)
        {
            if (result == null)
                throw new OpcException("The server replied with an empty response");
            if (result.ResultID.ToString() != "S_OK")
                throw new OpcException(string.Format("Invalid response from the server. (Response Status: {0}, Opc Tag: {1})", result.ResultID, tag));
        }
    }
}

