using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace Esp32Buttons
{
    /// <summary>
    /// Five momentary push buttons, each toggling its own LED on press.
    ///
    /// Wiring (default pins, change the arrays below to match your board):
    ///   Button 1: GPIO 15  ->  LED 1: GPIO 16
    ///   Button 2: GPIO 4   ->  LED 2: GPIO 17
    ///   Button 3: GPIO 5   ->  LED 3: GPIO 19
    ///   Button 4: GPIO 18  ->  LED 4: GPIO 21
    ///   Button 5: GPIO 23  ->  LED 5: GPIO 22
    ///
    /// Buttons use the ESP32's internal pull-ups: wire each button between
    /// its GPIO and GND (pressed = LOW). Wire each LED from its GPIO through
    /// a ~330 ohm resistor to GND (HIGH = on).
    /// </summary>
    public class Program
    {
        private static readonly int[] ButtonPins = { 15, 4, 5, 18, 23 };
        private static readonly int[] LedPins = { 16, 17, 19, 21, 22 };

        // Test mode: blink the onboard LED (GPIO 2 on most dev boards) N times
        // when button N is pressed. Lets you verify buttons with no LEDs wired.
        private const bool OnboardLedFeedback = false;
        private const int OnboardLedPin = 2;

        // Lockout window after an accepted press: further edges from this
        // button are ignored until it elapses. Filters contact bounce on both
        // press and release (release bounce was re-toggling LEDs at 50 ms).
        private const long PressLockoutTicks = 250 * TimeSpan.TicksPerMillisecond;

        private static GpioController s_gpio;
        private static long[] s_lastPressTicks;
        private static bool[] s_ledStates;
        private static int s_blinkActive;

        public static void Main()
        {
            Debug.WriteLine("ESP32 Buttons starting...");

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

                // Buttons are wired to GND, so a press is a falling edge.
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

            // Ignore edges that arrive while this button's lockout is active.
            long now = DateTime.UtcNow.Ticks;
            if (now - s_lastPressTicks[index] < PressLockoutTicks)
            {
                return;
            }

            s_lastPressTicks[index] = now;

            // Track LED state in software: reading back an output pin on the
            // ESP32 does not reliably return the driven value.
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

        /// <summary>
        /// Blinks the onboard LED on a background thread so the GPIO event
        /// thread is never blocked and button presses stay responsive.
        /// If a blink sequence is already running, the new one is skipped
        /// (the LED toggle above has already happened regardless).
        /// </summary>
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
