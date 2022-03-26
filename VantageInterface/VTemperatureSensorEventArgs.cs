using System;
using System.Collections.Generic;
using System.Text;

namespace VantageInterface
{
    public class VTemperatureSensorEventArgs : VEventArgs
    {
        public TemperatureSensors Sensor { get; }
        public float Value { get; }

        public VTemperatureSensorEventArgs(int vid, TemperatureSensors sensor, float value)
            : base(vid, VEventType.TemperatureSensorUpdate)
        {
            Sensor = sensor;
            Value = value;
        }
    }
}
