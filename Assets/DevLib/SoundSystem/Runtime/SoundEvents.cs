using DevLib.CoreSystem;
using UnityEngine;

namespace DevLib.SoundSystem.Runtime
{
    public static class SoundEvents
    {
        public static readonly PlaySoundEvent PlaySoundEvent = new PlaySoundEvent();
        public static readonly StopSoundEvent StopSoundEvent = new StopSoundEvent();
    }

    public class PlaySoundEvent : GameEvent
    {
        public Vector3 Position;
        public SoundClipSO ClipData;
        public int ChannelNumber;

        public PlaySoundEvent Init(Vector3 position, SoundClipSO clipData, int channelNumber = 0)
        {
            Position = position;
            ClipData = clipData;
            ChannelNumber = channelNumber;
            return this;
        }
    }

    public class StopSoundEvent : GameEvent
    {
        public int ChannelNumber;

        public StopSoundEvent Init(int channelNumber = 0)
        {
            ChannelNumber = channelNumber;
            return this;
        }
    }

}