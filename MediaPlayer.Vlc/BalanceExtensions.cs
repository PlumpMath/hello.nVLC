﻿using System;
using NAudio.CoreAudioApi;

namespace MediaPlayer.Vlc
{
    public static class BalanceExtensions
    {
        // TODO: control via IAudioStreamVolume, IChannelAudioVolume, i.e.
        //
        // IAudioClient.GetService(REFIID id, out iface):
        //
        // - IAudioStreamVolume: https://msdn.microsoft.com/en-us/library/windows/desktop/dd370977(v=vs.85).aspx
        //      IID_IAudioStreamVolume
        //
        // - IChannelAudioVolume: https://msdn.microsoft.com/en-us/library/windows/desktop/dd371023(v=vs.85).aspx
        //      IID_IChannelAudioVolume
        // -

        // left/right should be 0/1. But it varies. Try out for yourself!
        private const int LeftChan = 0;
        private const int RightChan = 1;

        private static MMDevice _device;

        public static MMDevice GetDefaultDevice()
        {
            return _device ?? (_device = new MMDeviceEnumerator()
                .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
        }

        #region AudioEndpointVolume

        /// <summary>
        /// Gets the ratio of volume across the left and right speakers in a range between -1 (left) and 1 (right). The default is 0 (center).
        /// </summary>
        /// <param name="volume">The audio volume endpoint</param>
        /// <returns></returns>
        public static float GetBalance(this AudioEndpointVolume volume)
        {
            VerifyChannels(volume);

            var masterVol = Math.Max(1e-6f, volume.MasterVolumeLevelScalar);

            var leftVol = volume.Channels[LeftChan].VolumeLevelScalar;
            var rightVol = volume.Channels[RightChan].VolumeLevelScalar;

            var balance = (rightVol - leftVol) / masterVol;
            return balance;
        }

        public static void SetBalance(this AudioEndpointVolume volume, float balance)
        {
            VerifyChannels(volume);

            var safeBalance = Math.Max(-1, Math.Min(1, balance));
            var masterVol = volume.MasterVolumeLevelScalar;
            var rightVol = 1f + Math.Min(0f, safeBalance);
            var leftVol = 1f - Math.Max(0f, safeBalance);

            volume.Channels[LeftChan].VolumeLevelScalar = leftVol * masterVol;
            volume.Channels[RightChan].VolumeLevelScalar = rightVol * masterVol;
        }

        public static AudioEndpointVolume GetDefaultVolumeEndpoint()
        {
            return GetDefaultDevice()
                .AudioEndpointVolume;
        }

        private static void VerifyChannels(AudioEndpointVolume volume)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (volume.Channels.Count < 2)
                throw new InvalidOperationException("The specified audio endpoint does not expose left/right volume channels");
        }

        #endregion

        #region AudioStreamVolume

        public static float GetBalance(this AudioStreamVolume volume)
        {
            VerifyChannels(volume);

            var leftVol = volume.GetChannelVolume(LeftChan);
            var rightVol = volume.GetChannelVolume(RightChan);

            var masterVol = Math.Max(leftVol, rightVol);
            if (masterVol < 1e-6f) return 0f;

            var balance = (rightVol - leftVol) / masterVol;
            return balance;
        }

        public static void SetBalance(this AudioStreamVolume volume, float balance)
        {
            var safeBalance = Math.Max(-1, Math.Min(1, balance));
            var rightVol = 1f + Math.Min(0f, safeBalance);
            var leftVol = 1f - Math.Max(0f, safeBalance);

            volume.SetChannelVolume(LeftChan, leftVol);
            volume.SetChannelVolume(RightChan, rightVol);
        }

        private static void VerifyChannels(AudioStreamVolume volume)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (volume.ChannelCount < 2)
                throw new InvalidOperationException("The specified audio endpoint does not expose left/right volume channels");
        }

        #endregion
    }
}