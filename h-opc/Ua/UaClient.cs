using Hylasoft.Opc.Common;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hylasoft.Opc.Ua
{
    /// <summary>
    /// Client Implementation for UA
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling",
      Justification = "Doesn't make sense to split this class")]
    public class UaClient : IClient
    {
        private readonly UaClientOptions _options = new UaClientOptions();
        private readonly Uri _serverUrl;
        private Session _session;

        /// <summary>
        /// Creates a server object
        /// </summary>
        /// <param name="serverUrl">the url of the server to connect to</param>
        public UaClient(Uri serverUrl)
        {
            this._serverUrl = serverUrl;
            this.Status = OpcStatus.NotConnected;
        }

        /// <summary>
        /// Creates a server object
        /// </summary>
        /// <param name="serverUrl">the url of the server to connect to</param>
        /// <param name="options">custom options to use with ua client</param>
        public UaClient(Uri serverUrl, UaClientOptions options)
        {
            this._serverUrl = serverUrl;
            this._options = options;
            this.Status = OpcStatus.NotConnected;
        }

        /// <summary>
        /// Options to configure the UA client session
        /// </summary>
        public UaClientOptions Options
        {
            get { return this._options; }
        }

        /// <summary>
        /// OPC Foundation underlying session object
        /// </summary>
        protected Session Session
        {
            get
            {
                return this._session;
            }
        }

        private void PostInitializeSession()
        {
            this.Status = OpcStatus.Connected;
        }

        /// <summary>
        /// Connect the client to the OPC Server
        /// </summary>
        public void Connect()
        {
            if (this.Status == OpcStatus.Connected)
                return;
            this._session = this.InitializeSession(this._serverUrl);
            this._session.KeepAlive += this.SessionKeepAlive;
            this._session.SessionClosing += this.SessionClosing;

            this.PostInitializeSession();
        }

        /// <summary>
        /// Gets the datatype of an OPC tag
        /// </summary>
        /// <param name="tag">Tag to get datatype of</param>
        /// <returns>System Type</returns>
        public Type GetDataType(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);
            DataValueCollection results;
            DiagnosticInfoCollection diag;
            this._session.Read(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                results: out results,
                diagnosticInfos: out diag);
            BuiltInType type = results[0].WrappedValue.TypeInfo.BuiltInType;
            return Type.GetType("System." + type.ToString());
        }

        private void SessionKeepAlive(Session session, KeepAliveEventArgs e)
        {
            if (e.CurrentState != ServerState.Running)
            {
                if (this.Status == OpcStatus.Connected)
                {
                    this.Status = OpcStatus.NotConnected;
                    this.NotifyServerConnectionLost();
                }
            }
            else if (e.CurrentState == ServerState.Running)
            {
                if (this.Status == OpcStatus.NotConnected)
                {
                    this.Status = OpcStatus.Connected;
                    this.NotifyServerConnectionRestored();
                }
            }
        }

        private void SessionClosing(object sender, EventArgs e)
        {
            this.Status = OpcStatus.NotConnected;
            this.NotifyServerConnectionLost();
        }


        /// <summary>
        /// Reconnect the OPC session
        /// </summary>
        public void ReConnect()
        {
            this.Status = OpcStatus.NotConnected;
            this._session.Reconnect();
            this.Status = OpcStatus.Connected;
        }

        /// <summary>
        /// Create a new OPC session, based on the current session parameters.
        /// </summary>
        public void RecreateSession()
        {
            this.Status = OpcStatus.NotConnected;
            this._session = Session.Recreate(this._session);
            this.PostInitializeSession();
        }


        /// <summary>
        /// Gets the current status of the OPC Client
        /// </summary>
        public OpcStatus Status { get; private set; }


        private ReadValueIdCollection BuildReadValueIdCollection(string tag, uint attributeId)
        {
            //Updated by Lee
            NodeId nodeId = new NodeId(tag);
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId()
                {
                    NodeId = nodeId,
                    AttributeId = attributeId
                }
            };

            return nodesToRead;
        }

        /// <summary>
        /// Read a tag
        /// </summary>
        /// <typeparam name="T">The type of tag to read</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        public ReadEvent<T> Read<T>(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);
            DataValueCollection results;
            DiagnosticInfoCollection diag;
            this._session.Read(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                results: out results,
                diagnosticInfos: out diag);
            DataValue val = results[0];

            ReadEvent<T> readEvent = new ReadEvent<T>();
            readEvent.Value = (T)val.Value;
            readEvent.SourceTimestamp = val.SourceTimestamp;
            readEvent.ServerTimestamp = val.ServerTimestamp;
            if (StatusCode.IsGood(val.StatusCode)) readEvent.Quality = Quality.Good;
            if (StatusCode.IsBad(val.StatusCode)) readEvent.Quality = Quality.Bad;
            return readEvent;
        }


        /// <summary>
        /// Read a tag asynchronously
        /// </summary>
        /// <typeparam name="T">The type of tag to read</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` reads the tag `bar` on the folder `foo`</param>
        /// <returns>The value retrieved from the OPC</returns>
        public Task<ReadEvent<T>> ReadAsync<T>(string tag)
        {
            ReadValueIdCollection nodesToRead = this.BuildReadValueIdCollection(tag, Attributes.Value);

            // Wrap the ReadAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
            TaskCompletionSource<ReadEvent<T>> taskCompletionSource = new TaskCompletionSource<ReadEvent<T>>();
            this._session.BeginRead(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                callback: ar =>
                {
                    DataValueCollection results;
                    DiagnosticInfoCollection diag;
                    ResponseHeader response = this._session.EndRead(
                  result: ar,
                  results: out results,
                  diagnosticInfos: out diag);

                    try
                    {
                        this.CheckReturnValue(response.ServiceResult);
                        DataValue val = results[0];
                        ReadEvent<T> readEvent = new ReadEvent<T>();
                        readEvent.Value = (T)val.Value;
                        readEvent.SourceTimestamp = val.SourceTimestamp;
                        readEvent.ServerTimestamp = val.ServerTimestamp;
                        if (StatusCode.IsGood(val.StatusCode)) readEvent.Quality = Quality.Good;
                        if (StatusCode.IsBad(val.StatusCode)) readEvent.Quality = Quality.Bad;
                        taskCompletionSource.TrySetResult(readEvent);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                },
                asyncState: null);

            return taskCompletionSource.Task;
        }


        private WriteValueCollection BuildWriteValueCollection(string tag, uint attributeId, object dataValue)
        {
            //Updated by Lee
            WriteValue valueToWrite = new WriteValue
            {
                NodeId = new NodeId(tag),
                AttributeId = attributeId,
                Value = { Value = dataValue }
            };

            WriteValueCollection valuesToWrite = new WriteValueCollection { valueToWrite };

            return valuesToWrite;
        }

        /// <summary>
        /// Write a value on the specified opc tag
        /// </summary>
        /// <typeparam name="T">The type of tag to write on</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` writes on the tag `bar` on the folder `foo`</param>
        /// <param name="item">The value for the item to write</param>
        public void Write<T>(string tag, T item)
        {
            WriteValueCollection nodesToWrite = this.BuildWriteValueCollection(tag, Attributes.Value, item);

            StatusCodeCollection results;
            DiagnosticInfoCollection diag;
            this._session.Write(
                requestHeader: null,
                nodesToWrite: nodesToWrite,
                results: out results,
                diagnosticInfos: out diag);

            this.CheckReturnValue(results[0]);
        }

        /// <summary>
        /// Write a value on the specified opc tag asynchronously
        /// </summary>
        /// <typeparam name="T">The type of tag to write on</typeparam>
        /// <param name="tag">The fully-qualified identifier of the tag. You can specify a subfolder by using a comma delimited name.
        /// E.g: the tag `foo.bar` writes on the tag `bar` on the folder `foo`</param>
        /// <param name="item">The value for the item to write</param>
        public Task WriteAsync<T>(string tag, T item)
        {
            WriteValueCollection nodesToWrite = this.BuildWriteValueCollection(tag, Attributes.Value, item);

            // Wrap the WriteAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
            TaskCompletionSource<StatusCode> taskCompletionSource = new TaskCompletionSource<StatusCode>();
            this._session.BeginWrite(
                requestHeader: null,
                nodesToWrite: nodesToWrite,
                callback: ar =>
                {
                    StatusCodeCollection results;
                    DiagnosticInfoCollection diag;
                    ResponseHeader response = this._session.EndWrite(
                  result: ar,
                  results: out results,
                  diagnosticInfos: out diag);
                    try
                    {
                        this.CheckReturnValue(response.ServiceResult);
                        this.CheckReturnValue(results[0]);
                        taskCompletionSource.SetResult(response.ServiceResult);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                },
                asyncState: null);
            return taskCompletionSource.Task;
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
            //Updated by Lee
            NodeId nodeId = new NodeId(tag);

            Subscription sub = new Subscription
            {
                PublishingInterval = this._options.DefaultMonitorInterval,
                PublishingEnabled = true,
                LifetimeCount = this._options.SubscriptionLifetimeCount,
                KeepAliveCount = this._options.SubscriptionKeepAliveCount,
                DisplayName = tag,
                Priority = byte.MaxValue
            };

            MonitoredItem item = new MonitoredItem
            {
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                DisplayName = tag,
                SamplingInterval = this._options.DefaultMonitorInterval
            };
            sub.AddItem(item);
            this._session.AddSubscription(sub);
            sub.Create();
            sub.ApplyChanges();

            item.Notification += (monitoredItem, args) =>
            {
                MonitoredItemNotification p = (MonitoredItemNotification)args.NotificationValue;
                object t = p.Value.WrappedValue.Value;
                Action unsubscribe = () =>
                {
                    sub.RemoveItems(sub.MonitoredItems);
                    sub.Delete(true);
                    this._session.RemoveSubscription(sub);
                    sub.Dispose();
                };

                ReadEvent<T> monitorEvent = new ReadEvent<T>();
                monitorEvent.Value = (T)t;
                monitorEvent.SourceTimestamp = p.Value.SourceTimestamp;
                monitorEvent.ServerTimestamp = p.Value.ServerTimestamp;
                if (StatusCode.IsGood(p.Value.StatusCode)) monitorEvent.Quality = Quality.Good;
                if (StatusCode.IsBad(p.Value.StatusCode)) monitorEvent.Quality = Quality.Bad;
                callback(monitorEvent, unsubscribe);
            };
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this._session != null)
            {
                this._session.RemoveSubscriptions(this._session.Subscriptions.ToList());
                this._session.Close();
                this._session.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        private void CheckReturnValue(StatusCode status)
        {
            if (!StatusCode.IsGood(status))
                throw new OpcException(string.Format("Invalid response from the server. (Response Status: {0})", status), status);
        }

        /// <summary>
        /// Return identity login object for a given URI.
        /// </summary>
        /// <param name="url">Login URI</param>
        /// <returns>AnonUser or User with name and password</returns>
        private UserIdentity GetIdentity(Uri url)
        {
            if (this._options.UserIdentity != null)
            {
                return this._options.UserIdentity;
            }
            UserIdentity uriLogin = new UserIdentity();
            if (!string.IsNullOrEmpty(url.UserInfo))
            {
                string[] uis = url.UserInfo.Split(':');
                uriLogin = new UserIdentity(uis[0], uis[1]);
            }
            return uriLogin;
        }

        /// <summary>
        /// Crappy method to initialize the session. I don't know what many of these things do, sincerely.
        /// </summary>
        private Session InitializeSession(Uri url)
        {
            CertificateValidator certificateValidator = new CertificateValidator();
            certificateValidator.CertificateValidation += (sender, eventArgs) =>
            {
                if (ServiceResult.IsGood(eventArgs.Error))
                    eventArgs.Accept = true;
                else if ((eventArgs.Error.StatusCode.Code == StatusCodes.BadCertificateUntrusted) && this._options.AutoAcceptUntrustedCertificates)
                    eventArgs.Accept = true;
                else
                    throw new OpcException(string.Format("Failed to validate certificate with error code {0}: {1}", eventArgs.Error.Code, eventArgs.Error.AdditionalInfo), eventArgs.Error.StatusCode);
            };
            // Build the application configuration
            ApplicationInstance appInstance = new ApplicationInstance
            {
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = this._options.ConfigSectionName,
                ApplicationConfiguration = new ApplicationConfiguration
                {
                    ApplicationUri = url.ToString(),
                    ApplicationName = this._options.ApplicationName,
                    ApplicationType = ApplicationType.Client,
                    CertificateValidator = certificateValidator,
                    ServerConfiguration = new ServerConfiguration
                    {
                        MaxSubscriptionCount = this._options.MaxSubscriptionCount,
                        MaxMessageQueueSize = this._options.MaxMessageQueueSize,
                        MaxNotificationQueueSize = this._options.MaxNotificationQueueSize,
                        MaxPublishRequestCount = this._options.MaxPublishRequestCount
                    },
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = this._options.AutoAcceptUntrustedCertificates
                    },
                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 600000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 600000,
                        SecurityTokenLifetime = 3600000
                    },
                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000,
                        MinSubscriptionLifetime = 10000
                    },
                    DisableHiResClock = true
                }
            };

            // Assign a application certificate (when specified)
            if (this._options.ApplicationCertificate != null)
                appInstance.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier(this._options.ApplicationCertificate);

            // Find the endpoint to be used
            EndpointDescription endpoints = ClientUtils.SelectEndpoint(url, this._options.UseMessageSecurity);

            // Create the OPC session:
            Session session = Session.Create(
                configuration: appInstance.ApplicationConfiguration,
                endpoint: new ConfiguredEndpoint(
                    collection: null,
                    description: endpoints,
                    configuration: EndpointConfiguration.Create(applicationConfiguration: appInstance.ApplicationConfiguration)),
                updateBeforeConnect: false,
                checkDomain: false,
                sessionName: this._options.SessionName,
                sessionTimeout: this._options.SessionTimeout,
                identity: this.GetIdentity(url),
                preferredLocales: new string[] { });

            return session;
        }

        private void NotifyServerConnectionLost()
        {
            if (this.ServerConnectionLost != null) this.ServerConnectionLost(this, EventArgs.Empty);
        }

        private void NotifyServerConnectionRestored()
        {
            if (this.ServerConnectionRestored != null) this.ServerConnectionRestored(this, EventArgs.Empty);
        }

        /// <summary>
        /// This event is raised when the connection to the OPC server is lost.
        /// </summary>
        public event EventHandler ServerConnectionLost;

        /// <summary>
        /// This event is raised when the connection to the OPC server is restored.
        /// </summary>
        public event EventHandler ServerConnectionRestored;

    }

}
