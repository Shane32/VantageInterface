using System;
using System.Collections.Generic;
using System.Text;

namespace VantageInterface
{
    internal static class VantageParser
    {
        public static (int vid, float percent) ParseLoad(this string value)
        {
            string[] ret2 = value.Split(' ');
            int vid = int.Parse(ret2[1]);
            float percent = float.Parse(ret2[2]);
            return (vid, percent);
        }

        public static (int vid, int newState) ParseTask(this string value)
        {
            string[] ret2 = value.Split(' ');
            int vid = int.Parse(ret2[1]);
            int newState = int.Parse(ret2[2]);
            return (vid, newState);
        }

        public static (int vid, ButtonModes mode) ParseButton(this string value)
        {
            string[] ret2 = value.Split(' ');
            int vid = int.Parse(ret2[1]);
            var mode = ret2[2] == "PRESS" ? ButtonModes.Press : ret2[2] == "RELEASE" ? ButtonModes.Release : throw new FormatException();
            return (vid, mode);
        }

        public static (int Vid, LedState State) ParseLed(this string value)
        {
            string[] ret2 = value.Split(' ');
            int vid = int.Parse(ret2[1]);
            int mode = int.Parse(ret2[2]);
            byte red1 = byte.Parse(ret2[3]);
            byte green1 = byte.Parse(ret2[4]);
            byte blue1 = byte.Parse(ret2[5]);
            byte red2 = byte.Parse(ret2[6]);
            byte blue2 = byte.Parse(ret2[7]);
            byte green2 = byte.Parse(ret2[8]);
            BlinkRates blinkRate;
            switch(ret2[9])
            {
                case "FAST": blinkRate = BlinkRates.Fast; break;
                case "MEDIUM": blinkRate = BlinkRates.Fast; break;
                case "SLOW": blinkRate = BlinkRates.Fast; break;
                case "VERYSLOW": blinkRate = BlinkRates.Fast; break;
                case "OFF": blinkRate = BlinkRates.Fast; break;
                default: throw new InvalidOperationException("Cannot parse blink rate");
            }
            return (vid, new LedState(mode, red1, blue1, green1, red2, blue2, green2, blinkRate));
        }
    }
}
