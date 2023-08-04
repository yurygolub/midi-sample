using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.Midi;

namespace MidiSample
{
    internal class Program
    {
        private const int MidiInDeviceIndex = 0;
        private const int MidiOutDeviceIndex = 1;

        private const string OutPath = "out.mid";
        private const string MidiFilePath = @"Linkin Park - Numb (Tim Dawes Remix).mid";

        private const int MidiFileType = 0;
        private const int DeltaTicksPerQuarterNote = 120;
        private static readonly MidiEventCollection MidiEvents = new (MidiFileType, DeltaTicksPerQuarterNote);

        private static readonly Stopwatch Stopwatch = new ();
        private static long absoluteTime;

        private static void Main()
        {
            Events();
            Files();
        }

        private static void Events()
        {
            Console.WriteLine("Events example:");
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

            var midiIn = new MidiIn(MidiInDeviceIndex);
            midiIn.MessageReceived += MidiIn_MessageReceived;
            midiIn.ErrorReceived += MidiIn_ErrorReceived;
            midiIn.Start();

            var midiOut = new MidiOut(MidiOutDeviceIndex);
            int channel = 1;
            int noteNumber = 50;
            var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
            midiOut.Send(noteOnEvent.GetAsShortMessage());
            midiOut.Dispose();

            Console.WriteLine("Press escape to exit");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }

            midiIn.Stop();
            midiIn.Dispose();

            MidiEvents.PrepareForExport();

            MidiFile.Export(OutPath, MidiEvents);
            Console.WriteLine($"Notes exported to file: {Path.GetFullPath(OutPath)}");
            Console.WriteLine();
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

                if (e.MidiEvent is NoteOnEvent noteOnEvent)
                {
                    Stopwatch.Stop();
                    absoluteTime += Stopwatch.ElapsedMilliseconds;
                    noteOnEvent.AbsoluteTime = absoluteTime;
                    noteOnEvent.NoteLength = 100;
                    MidiEvents.AddEvent(noteOnEvent, 0);
                    Stopwatch.Restart();
                }
            }
        }

        private static void Files()
        {
            Console.WriteLine("Files example:");
            bool strictMode = false;
            var midiFile = new MidiFile(MidiFilePath, strictMode);

            Console.WriteLine($"Format {midiFile.FileFormat}, Tracks {midiFile.Tracks}, Delta Ticks Per Quarter Note {midiFile.DeltaTicksPerQuarterNote}");

            TimeSignatureEvent timeSignature = midiFile.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();

            var midiOut = new MidiOut(MidiOutDeviceIndex);
            int absoluteTime = 0;

            for (int i = 0; i < midiFile.Tracks; i++)
            {
                foreach (MidiEvent midiEvent in midiFile.Events[i])
                {
                    if (!MidiEvent.IsNoteOff(midiEvent))
                    {
                        Console.WriteLine($"{ToMeasuresBeatsTicks(midiEvent.AbsoluteTime, midiFile.DeltaTicksPerQuarterNote, timeSignature)} {midiEvent}");
                    }

                    if (midiEvent.AbsoluteTime > absoluteTime)
                    {
                        Thread.Sleep((int)midiEvent.AbsoluteTime - absoluteTime);
                        absoluteTime = (int)midiEvent.AbsoluteTime;
                    }

                    midiOut.Send(midiEvent.GetAsShortMessage());
                }
            }

            midiOut.Dispose();
        }

        private static string ToMeasuresBeatsTicks(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
        {
            int beatsPerBar = timeSignature == null
                ? 4
                : timeSignature.Numerator;

            int ticksPerBar = timeSignature == null
                ? ticksPerQuarterNote * 4
                : timeSignature.Numerator * ticksPerQuarterNote * 4 / (1 << timeSignature.Denominator);

            int ticksPerBeat = ticksPerBar / beatsPerBar;
            long bar = 1 + (eventTime / ticksPerBar);
            long beat = 1 + ((eventTime % ticksPerBar) / ticksPerBeat);
            long tick = eventTime % ticksPerBeat;

            return $"{bar}:{beat}:{tick}";
        }
    }
}
