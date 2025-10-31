using Automatic_Replay_Buffer.Models;
using Automatic_Replay_Buffer.ViewModel;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class OBSService
    {
        private readonly LoggingService LoggingService;
        private readonly OBSWebsocket OBSWebsocket = new();
        private readonly MainViewModel vm;
        private readonly Dispatcher Dispatcher = System.Windows.Application.Current.Dispatcher;

        public bool isActive;

        private string? _address;
        private string? _password;

        public OBSService(LoggingService _loggingService, MainViewModel _vm)
        {
            LoggingService = _loggingService;
            vm = _vm;

            OBSWebsocket.Connected += Obs_Connected;
            OBSWebsocket.Disconnected += Obs_Disconnected;

            OBSWebsocket.ReplayBufferStateChanged += (s, e) =>
            {
                isActive = e.OutputState.IsActive;
            };
        }

        public void Connect(string address, string password)
        {
            _address = address;
            _password = password;

            if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(password) && !OBSWebsocket.IsConnected)
            {
                vm.WebsocketState = ServiceState.Busy;
                LoggingService.Log("Attempting to connect to WebSocket...");

                Task.Run(() =>
                {
                    try
                    {
                        OBSWebsocket.ConnectAsync(address, password);
                    }
                    catch (Exception ex)
                    {
                        vm.WebsocketState = ServiceState.Offline;
                        LoggingService.Log($"Error when connecting to OBS WebSocket: {ex.Message}");
                    }
                });
            }
        }

        private void Obs_Connected(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                vm.WebsocketState = ServiceState.Online;
            });

            isActive = OBSWebsocket.GetReplayBufferStatus();
            LoggingService.Log("Connected to WebSocket");
        }

        private void Obs_Disconnected(object? sender, ObsDisconnectionInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                vm.WebsocketState = ServiceState.Offline;
            });

            isActive = false;
            LoggingService.Log("Disconnected from WebSocket");

            if (!string.IsNullOrEmpty(_address) && !string.IsNullOrEmpty(_password))
                Connect(_address, _password);
        }

        public void StartBuffer()
        {
            if (OBSWebsocket.IsConnected)
            {
                LoggingService.Log("Starting OBS Replay Buffer...");

                Task.Run(() =>
                {
                    try
                    {
                        OBSWebsocket.StartReplayBuffer();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log($"Error when starting Replay Buffer: {ex.Message}");
                    }
                });
            }
        }

        public void StopBuffer()
        {
            if (OBSWebsocket.IsConnected)
            {
                LoggingService.Log("Stopping OBS Replay Buffer...");

                Task.Run(() =>
                {
                    try
                    {
                        OBSWebsocket.StopReplayBuffer();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log($"Error when stopping Replay Buffer: {ex.Message}");
                    }
                });
            }
        }

        public void Disconnect()
        {
            if (OBSWebsocket.IsConnected)
            {
                LoggingService.Log("Disconnecting from WebSocket...");

                Task.Run(() =>
                {
                    try
                    {
                        OBSWebsocket.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log($"Error when disconnecting from OBS: {ex.Message}");
                    }
                });
            }
        }
    }
}
