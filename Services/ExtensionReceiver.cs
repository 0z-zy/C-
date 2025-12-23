using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace OLED_Customizer.Services
{
    public class ExtensionReceiver
    {
        private readonly ILogger<ExtensionReceiver> _logger;
        private HttpListener? _listener;
        private Thread? _listenerThread;
        private Dictionary<string, object>? _latestData;
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly object _lock = new object();
        private const int PORT = 2408;

        public ExtensionReceiver(ILogger<ExtensionReceiver> logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{PORT}/");
                _listener.Start();
                
                _listenerThread = new Thread(ListenLoop);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();
                
                _logger.LogInformation($"Extension Receiver listening on port {PORT}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start Extension Receiver: {ex.Message}");
            }
        }

        public Dictionary<string, object>? GetLatestData()
        {
            lock (_lock)
            {
                if (_latestData != null && (DateTime.Now - _lastUpdate).TotalSeconds < 5)
                {
                    return _latestData;
                }
                return null;
            }
        }

        private void ListenLoop()
        {
            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in listener loop: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // CORS
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/extension_data")
            {
                try
                {
                    using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                    var json = reader.ReadToEnd();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    lock (_lock)
                    {
                        // Clean up JSON elements if necessary (System.Text.Json deserializes numbers as JsonElement)
                        // For simplicity, we assume the display logic handles JsonElement or we convert here.
                        // But Dictionary<string, object> with System.Text.Json results in JsonElement values.
                        // We might want to convert them for easier usage.
                        
                        var cleanData = new Dictionary<string, object>();
                        if (data != null)
                        {
                            foreach (var kvp in data)
                            {
                                if (kvp.Value is JsonElement element)
                                {
                                    switch (element.ValueKind)
                                    {
                                        case JsonValueKind.String:
                                            cleanData[kvp.Key] = element.GetString() ?? "";
                                            break;
                                        case JsonValueKind.Number:
                                            if (element.TryGetInt32(out int i)) cleanData[kvp.Key] = i;
                                            else if (element.TryGetDouble(out double d)) cleanData[kvp.Key] = d;
                                            break;
                                        case JsonValueKind.True:
                                            cleanData[kvp.Key] = true;
                                            break;
                                        case JsonValueKind.False:
                                            cleanData[kvp.Key] = false;
                                            break;
                                        default:
                                            cleanData[kvp.Key] = element.ToString();
                                            break;
                                    }
                                }
                                else
                                {
                                    cleanData[kvp.Key] = kvp.Value;
                                }
                            }
                            _latestData = cleanData;
                            _lastUpdate = DateTime.Now;
                        }
                    }

                    response.StatusCode = 200;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to process extension data: {ex.Message}");
                    response.StatusCode = 400;
                }
            }
            else
            {
                response.StatusCode = 404;
            }

            response.Close();
        }
    }
}
