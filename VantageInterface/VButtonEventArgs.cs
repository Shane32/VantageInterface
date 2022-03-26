using System;
using System.Collections.Generic;
using System.Text;

namespace VantageInterface
{
    public class VButtonEventArgs : VEventArgs
    {
        public ButtonModes Mode { get; }

        public VButtonEventArgs(int vid, ButtonModes action)
            : base(vid, VEventType.ButtonUpdate)
        {
            Mode = action;
        }
    }
}
