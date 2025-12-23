using System;
using NAudio.CoreAudioApi;
using Microsoft.Extensions.Logging;

namespace OLED_Customizer.Services
{
    public class VolumeService : IDisposable
    {
        private readonly ILogger<VolumeService> _logger;
        private readonly MMDeviceEnumerator _enumerator;
        private MMDevice? _speaker;
        private MMDevice? _mic; // Communication device
        
        public event EventHandler? OnVolumeChanged;

        private float _lastVol = -1;
        private bool _lastMute = false;

        private (float volume, bool isMute, bool isMicMute) _cachedState = (0, false, false);

        public VolumeService(ILogger<VolumeService> logger)
        {
            _logger = logger;
            _enumerator = new MMDeviceEnumerator();
            Initialize();
            UpdateCache();
        }

        private void Initialize()
        {
            try
            {
                // Speakers (Multimedia Default)
                _speaker = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _speaker.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Speaker init failed: {ex.Message}");
            }

            try
            {
                // Mic (Communications Default)
                _mic = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                // Note: NAudio's VolumeNotification only works for Render devices reliably in some versions,
                // but we can poll Mic mute specifically if needed.
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Mic init failed: {ex.Message}");
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            UpdateCache();
            OnVolumeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCache()
        {
            float vol = 0;
            bool mute = false;
            bool micMute = false;

            if (_speaker != null)
            {
                try
                {
                    vol = _speaker.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                    mute = _speaker.AudioEndpointVolume.Mute;
                }
                catch { } 
            }

            if (_mic != null)
            {
                try
                {
                    micMute = _mic.AudioEndpointVolume.Mute;
                }
                catch { }
            }

            _cachedState = (vol, mute, micMute);
        }

        public (float volume, bool isMute, bool isMicMute) GetVolumeState()
        {
            return _cachedState;
        }

        public void ToggleMicMute()
        {
            if (_mic != null)
            {
                try
                {
                    _mic.AudioEndpointVolume.Mute = !_mic.AudioEndpointVolume.Mute;
                    OnVolumeChanged?.Invoke(this, EventArgs.Empty);  // Force update
                }
                catch { }
            }
        }

        public void Dispose()
        {
            if (_speaker != null)
            {
                _speaker.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
                _speaker.Dispose();
            }
            _mic?.Dispose();
            _enumerator.Dispose();
        }
    }
}
