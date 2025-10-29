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
        private readonly OBSWebsocket _OBSWebsocket = new();
        private readonly MainViewModel vm;
        public bool isActive;

        private string? _address;
        private string? _password;

        public OBSService(MainViewModel _vm)
        {
            vm = _vm;
            _OBSWebsocket.Connected += Obs_Connected;
            _OBSWebsocket.Disconnected += Obs_Disconnected;

            _OBSWebsocket.ReplayBufferStateChanged += (s, e) =>
            {
                isActive = e.OutputState.IsActive;
            };
        }

        public void Connect(string address, string password)
        {
            _address = address;
            _password = password;

            if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(password) && !_OBSWebsocket.IsConnected)
            {
                vm.WebsocketText = "Connecting";
                vm._LoggingService.Log("Attempting to connect to WebSocket...");
                Debug.WriteLine("Attempting to connect to WebSocket...");

                try
                {
                    _OBSWebsocket.ConnectAsync(address, password);
                }
                catch (Exception ex)
                {
                    vm.WebsocketText = "Disconnected";
                    vm._LoggingService.Log($"Error when connecting to OBS WebSocket: {ex.Message}");
                    Debug.WriteLine($"Error when connecting to OBS WebSocket: {ex.Message}");
                }
            }
        }

        private void Obs_Connected(object? sender, EventArgs e)
        {
            vm.WebsocketText = "Connected";
            isActive = _OBSWebsocket.GetReplayBufferStatus();

            vm._LoggingService.Log("Connected to WebSocket");
            Debug.WriteLine("Connected to WebSocket");
        }

        private void Obs_Disconnected(object? sender, ObsDisconnectionInfo e)
        {
            vm.WebsocketText = "Disconnected";
            isActive = false;

            vm._LoggingService.Log("Disconnected from WebSocket");
            Debug.WriteLine("Disconnected from WebSocket");

            if (!string.IsNullOrEmpty(_address) && !string.IsNullOrEmpty(_password))
                Connect(_address, _password);
        }

        public void StartBuffer()
        {
            if (_OBSWebsocket.IsConnected)
            {
                vm._LoggingService.Log("Starting OBS Replay Buffer...");
                Debug.WriteLine("Starting OBS Replay Buffer...");

                try
                {
                    _OBSWebsocket.StartReplayBuffer();
                }
                catch (Exception ex)
                {
                    vm._LoggingService.Log($"Error when starting Replay Buffer: {ex.Message}");
                    Debug.WriteLine($"Error when starting Replay Buffer: {ex.Message}");
                }
            }
        }

        public void StopBuffer()
        {
            if (_OBSWebsocket.IsConnected)
            {
                vm._LoggingService.Log("Stopping OBS Replay Buffer...");
                Debug.WriteLine("Stopping OBS Replay Buffer...");

                try
                {
                    _OBSWebsocket.StopReplayBuffer();
                }
                catch (Exception ex)
                {
                    vm._LoggingService.Log($"Error when stopping Replay Buffer: {ex.Message}");
                    Debug.WriteLine($"Error when stopping Replay Buffer: {ex.Message}");
                }
            }
        }

        public void Disconnect()
        {
            if (_OBSWebsocket.IsConnected)
            {
                vm._LoggingService.Log("Disconnecting from WebSocket...");
                Debug.WriteLine("Disconnecting from WebSocket...");

                try
                {
                    _OBSWebsocket.Disconnect();
                }
                catch (Exception ex)
                {
                    vm._LoggingService.Log($"Error when disconnecting from OBS: {ex.Message}");
                    Debug.WriteLine($"Error when disconnecting from OBS: {ex.Message}");
                }
            }
        }
    }
}
