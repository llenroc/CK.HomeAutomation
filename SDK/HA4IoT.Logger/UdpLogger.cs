﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking;
using Windows.Networking.Sockets;
using HA4IoT.Contracts.Api;
using HA4IoT.Contracts.Logging;
using HA4IoT.Networking;

namespace HA4IoT.Logger
{
    public class UdpLogger : ILogger
    {
        private readonly bool _isDebuggerAttached = Debugger.IsAttached;

        private readonly object _syncRoot = new object();
        private readonly List<LogEntry> _history = new List<LogEntry>();

        private List<LogEntry> _items = new List<LogEntry>();
        private List<LogEntry> _itemsBuffer = new List<LogEntry>();

        private long _currentId;

        public UdpLogger()
        {
            Task.Factory.StartNew(
                SendQueuedItems, 
                CancellationToken.None, 
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void ExposeToApi(IApiController apiController)
        {
            if (apiController == null) throw new ArgumentNullException(nameof(apiController));

            apiController.RouteRequest("trace", HandleApiGet);
        }

        public void Verbose(string message)
        {
            Publish(LogEntrySeverity.Verbose, message);
        }

        public void Info(string message)
        {
            Publish(LogEntrySeverity.Info, message);
        }

        public void Warning(string message)
        {
            Publish(LogEntrySeverity.Warning, message);
        }

        public void Warning(Exception exception, string message)
        {
            Publish(LogEntrySeverity.Warning, message + Environment.NewLine + exception);
        }

        public void Error(string message)
        {
            Publish(LogEntrySeverity.Error, message);
        }

        public void Error(Exception exception, string message)
        {
            Publish(LogEntrySeverity.Error, message + Environment.NewLine + exception);
        }

        private void Publish(LogEntrySeverity type, string message, params object[] parameters)
        {
            if (parameters != null && parameters.Any())
            {
                try
                {
                    message = string.Format(message, parameters);
                }
                catch (FormatException)
                {
                    message = message + " (" + string.Join(",", parameters) + ")";
                }
            }

            PrintNotification(type, message);

            // TODO: Refactor to use IHomeAutomationTimer.CurrentDateTime;
            var logEntry = new LogEntry(_currentId, DateTime.Now, Environment.CurrentManagedThreadId, type, string.Empty, message);
            lock (_syncRoot)
            {
                _items.Add(logEntry);
                _currentId++;

                if (logEntry.Severity != LogEntrySeverity.Verbose)
                {
                    _history.Add(logEntry);

                    if (_history.Count > 100)
                    {
                        _history.RemoveAt(0);
                    }
                }
            }
        }

        private void HandleApiGet(IApiContext apiContext)
        {
            lock (_syncRoot)
            {
                apiContext.Response = CreatePackage(_history);
            }
        }

        private async Task SendQueuedItems()
        {
            using (DatagramSocket socket = new DatagramSocket())
            {
                socket.Control.DontFragment = true;
                await socket.ConnectAsync(new HostName("255.255.255.255"), "19227");

                using (Stream outputStream = socket.OutputStream.AsStreamForWrite())
                {
                    while (true)
                    {
                        List<LogEntry> pendingItems = GetPendingItems();
                        try
                        {
                            foreach (var traceItem in pendingItems)
                            {
                                var collection = new[] {traceItem};
                                JsonObject package = CreatePackage(collection);

                                string data = package.Stringify();
                                byte[] buffer = Encoding.UTF8.GetBytes(data);

                                outputStream.Write(buffer, 0, buffer.Length);
                                outputStream.Flush();
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine("ERROR: Could not send trace items. " + exception);
                        }
                        finally
                        {
                            pendingItems.Clear();
                        }

                        await Task.Delay(50);
                    }
                }
            }
        }

        private List<LogEntry> GetPendingItems()
        {
            lock (_syncRoot)
            {
                var buffer = _items;
                _items = _itemsBuffer;
                _itemsBuffer = buffer;

                return _itemsBuffer;
            }
        }

        private JsonObject CreatePackage(IEnumerable<LogEntry> traceItems)
        {
            var traceItemsCollection = new JsonArray();
            foreach (var traceItem in traceItems)
            {
                traceItemsCollection.Add(traceItem.ExportToJsonObject());
            }
            
            JsonObject package = new JsonObject();
            package.SetNamedValue("Type", "HA4IoT.Trace".ToJsonValue());
            package.SetNamedValue("Version", 1.ToJsonValue());
            package.SetNamedValue("TraceItems", traceItemsCollection);

            return package;
        }

        private void PrintNotification(LogEntrySeverity type, string message)
        {
            if (!_isDebuggerAttached)
            {
                return;
            }

            string typeText = string.Empty;
            switch (type)
            {
                case LogEntrySeverity.Error:
                    {
                        typeText = "ERROR";
                        break;
                    }

                case LogEntrySeverity.Info:
                    {
                        typeText = "INFO";
                        break;
                    }

                case LogEntrySeverity.Warning:
                    {
                        typeText = "WARNING";
                        break;
                    }

                case LogEntrySeverity.Verbose:
                    {
                        typeText = "VERBOSE";
                        break;
                    }
            }

            Debug.WriteLine(typeText + ": " + message);
        }
    }
}
