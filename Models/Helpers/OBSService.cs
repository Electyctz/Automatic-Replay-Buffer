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

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class OBSService
    {
        private readonly LoggingService LoggingService;
        private readonly OBSWebsocket OBSWebsocket = new();
        private readonly MainViewModel vm;
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
                vm.WebsocketText = "Connecting";
                LoggingService.Log("Attempting to connect to WebSocket...");

                try
                {
                    OBSWebsocket.ConnectAsync(address, password);
                }
                catch (Exception ex)
                {
                    vm.WebsocketText = "Disconnected";
                    LoggingService.Log($"Error when connecting to OBS WebSocket: {ex.Message}");
                }
            }
        }

        private void Obs_Connected(object? sender, EventArgs e)
        {
            vm.WebsocketText = "Connected";
            isActive = OBSWebsocket.GetReplayBufferStatus();

            LoggingService.Log("Connected to WebSocket");
        }

        private void Obs_Disconnected(object? sender, ObsDisconnectionInfo e)
        {
            vm.WebsocketText = "Disconnected";
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

                try
                {
                    OBSWebsocket.StartReplayBuffer();
                }
                catch (Exception ex)
                {
                    LoggingService.Log($"Error when starting Replay Buffer: {ex.Message}");
                }
            }
        }

        public void StopBuffer()
        {
            if (OBSWebsocket.IsConnected)
            {
                LoggingService.Log("Stopping OBS Replay Buffer...");

                try
                {
                    OBSWebsocket.StopReplayBuffer();
                }
                catch (Exception ex)
                {
                    LoggingService.Log($"Error when stopping Replay Buffer: {ex.Message}");
                }
            }
        }

        public void Disconnect()
        {
            if (OBSWebsocket.IsConnected)
            {
                LoggingService.Log("Disconnecting from WebSocket...");

                try
                {
                    OBSWebsocket.Disconnect();
                }
                catch (Exception ex)
                {
                    LoggingService.Log($"Error when disconnecting from OBS: {ex.Message}");
                }
            }
        }
    }
}
