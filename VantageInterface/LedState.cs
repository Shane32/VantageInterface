using System;
using System.Collections.Generic;
using System.Text;

namespace VantageInterface
{
    public struct LedState
    {
        public int State;
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte RedBlink;
        public byte GreenBlink;
        public byte BlueBlink;
        public BlinkRates BlinkMode;

        public LedState(int state, byte red, byte green, byte blue, byte redBlink, byte greenBlink, byte blueBlink, BlinkRates blinkMode)
        {
            State = state;
            Red = red;
            Green = green;
            Blue = blue;
            RedBlink = redBlink;
            GreenBlink = greenBlink;
            BlueBlink = blueBlink;
            BlinkMode = blinkMode;
        }
    }
}
