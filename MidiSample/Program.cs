using System;
using System.Linq;
using NAudio.Midi;

namespace MidiSample
{
    internal class Program
    {
        private static void Main()
        {
            MIDIEvents();
            MIDIFiles();
        }

        private static void MIDIEvents()
        {
            Console.WriteLine("MIDIEvents example:");
            Console.WriteLine("MidiIn:");
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                Console.WriteLine($"{device}: {MidiIn.DeviceInfo(device).ProductName}");
            }

            Console.WriteLine();

            Console.WriteLine("MidiOut:");
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                Console.WriteLine($"{device}: {MidiOut.DeviceInfo(device).ProductName}");
            }

            Console.WriteLine();

            int deviceIndex = 0;
            var midiIn = new MidiIn(deviceIndex);
            midiIn.MessageReceived += MidiIn_MessageReceived;
            midiIn.ErrorReceived += MidiIn_ErrorReceived;
            midiIn.Start();

            int midiOutDeviceIndex = 1;
            var midiOut = new MidiOut(midiOutDeviceIndex);
            int channel = 1;
            int noteNumber = 50;
            var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
            midiOut.Send(noteOnEvent.GetAsShortMessage());
            midiOut.Dispose();

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }

            midiIn.Stop();
            midiIn.Dispose();
        }

        private static void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine($"Error: Time {e.Timestamp} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
        }

        private static void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode != MidiCommandCode.TimingClock
                && e.MidiEvent.CommandCode != MidiCommandCode.AutoSensing)
            {
                Console.WriteLine($"Message: Time {e.Timestamp} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
            }
        }

        private static void MIDIFiles()
        {
            Console.WriteLine("MIDIFiles example:");
            var strictMode = false;
            string path = @"Linkin Park - Numb (Tim Dawes Remix).mid";
            var mf = new MidiFile(path, strictMode);

            Console.WriteLine($"Format {mf.FileFormat}, Tracks {mf.Tracks}, Delta Ticks Per Quarter Note {mf.DeltaTicksPerQuarterNote}");

            var timeSignature = mf.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();

            for (int n = 0; n < mf.Tracks; n++)
            {
                foreach (var midiEvent in mf.Events[n])
                {
                    if (!MidiEvent.IsNoteOff(midiEvent))
                    {
                        Console.WriteLine($"{ToMBT(midiEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote, timeSignature)} {midiEvent}");
                    }
                }
            }
        }

        private static string ToMBT(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
        {
            int beatsPerBar = timeSignature == null
                ? 4
                : timeSignature.Numerator;

            int ticksPerBar = timeSignature == null
                ? ticksPerQuarterNote * 4
                : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << timeSignature.Denominator);

            int ticksPerBeat = ticksPerBar / beatsPerBar;
            long bar = 1 + (eventTime / ticksPerBar);
            long beat = 1 + ((eventTime % ticksPerBar) / ticksPerBeat);
            long tick = eventTime % ticksPerBeat;

            return $"{bar}:{beat}:{tick}";
        }
    }
}
