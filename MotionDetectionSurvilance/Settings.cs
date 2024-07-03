using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetectionSurvilance
{
    public class Settings
    {
        public SettingName SettingName { get; set; }
        public int Value { get; set; }
    }
    public enum SettingName
    {
        Noise, Multiplier,NotificationAt,NotificationEnable
    }
}
