using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace PicoButtons
{
    /// <summary>
    /// Five momentary push buttons, each toggling its own LED on press.
    ///
    /// Default Raspberry Pi Pico wiring (RP2040 logical GPIO numbers):
    ///   Button 1: GPIO 2  -> LED 1: GPIO 7
    ///   Button 2: GPIO 3  -> LED 2: GPIO 8
    ///   Button 3: GPIO 4  -> LED 3: GPIO 9
    ///   Button 4: GPIO 5  -> LED 4: GPIO 10
    ///   Button 5: GPIO 6  -> LED 5: GPIO 11
    ///
    /// Buttons use internal pull-ups and are wired between GPIO and GND
    /// (pressed = LOW). LEDs are wired from GPIO through a current-limiting
    /// resistor to GND (HIGH = on).
    /// </summary>
    public class Program
    {
        private static readonly int[] ButtonPins = { 2, 3, 4, 5, 6 };
        private static readonly int[] LedPins = { 7, 8, 9, 10, 11 };

        // Optional diagnostic feedback. The regular Pico onboard LED is GPIO
        // 25; Pico W controls its LED separately, so leave this disabled for
        // the external five-LED configuration shared by both boards.
        private const bool OnboardLedFeedback = false;
        private const int OnboardLedPin = 25;

        private const long PressLockoutTicks = 250 * TimeSpan.TicksPerMillisecond;

        private static GpioController s_gpio;
        private static long[] s_lastPressTicks;
        private static bool[] s_ledStates;
        private static int s_blinkActive;

        public static void Main()
        {
            Debug.WriteLine("Raspberry Pi Pico Buttons starting...");

            s_gpio = new GpioController();
            s_lastPressTicks = new long[ButtonPins.Length];
            s_ledStates = new bool[LedPins.Length];

            for (int i = 0; i < LedPins.Length; i++)
            {
                s_gpio.OpenPin(LedPins[i], PinMode.Output);
                s_gpio.Write(LedPins[i], PinValue.Low);
            }

            if (OnboardLedFeedback)
            {
                s_gpio.OpenPin(OnboardLedPin, PinMode.Output);
                s_gpio.Write(OnboardLedPin, PinValue.Low);
            }

            for (int i = 0; i < ButtonPins.Length; i++)
            {
                s_gpio.OpenPin(ButtonPins[i], PinMode.InputPullUp);
                s_gpio.RegisterCallbackForPinValueChangedEvent(ButtonPins[i], PinEventTypes.Falling, OnButtonPressed);
            }

            Debug.WriteLine("Ready. Press a button to toggle its LED.");

            Thread.Sleep(Timeout.Infinite);
        }

        private static void OnButtonPressed(object sender, PinValueChangedEventArgs e)
        {
            int index = IndexOfButton(e.PinNumber);
            if (index < 0)
            {
                return;
            }

            long now = DateTime.UtcNow.Ticks;
            if (now - s_lastPressTicks[index] < PressLockoutTicks)
            {
                return;
            }

            s_lastPressTicks[index] = now;

            bool newOn = !s_ledStates[index];
            s_ledStates[index] = newOn;
            s_gpio.Write(LedPins[index], newOn ? PinValue.High : PinValue.Low);

            Debug.WriteLine("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff") + "] " +
                            "Button " + (index + 1) + " pressed (GPIO " + e.PinNumber + ") -> LED " +
                            (index + 1) + (newOn ? " ON" : " OFF"));

            if (OnboardLedFeedback)
            {
                StartBlinkOnboardLed(index + 1);
            }
        }

        private static void StartBlinkOnboardLed(int count)
        {
            if (Interlocked.CompareExchange(ref s_blinkActive, 1, 0) != 0)
            {
                return;
            }

            new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        s_gpio.Write(OnboardLedPin, PinValue.High);
                        Thread.Sleep(150);
                        s_gpio.Write(OnboardLedPin, PinValue.Low);
                        Thread.Sleep(150);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref s_blinkActive, 0);
                }
            }).Start();
        }

        private static int IndexOfButton(int pinNumber)
        {
            for (int i = 0; i < ButtonPins.Length; i++)
            {
                if (ButtonPins[i] == pinNumber)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
