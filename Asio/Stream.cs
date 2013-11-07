﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Asio
{
    class Stream : Audio.Stream
    {
        private class Buffer
        {
            private ASIOBufferInfo info;
            private ASIOSampleType type;
            private Audio.SampleBuffer samples;

            public ASIOBufferInfo Info { get { return info; } }
            public ASIOSampleType Type { get { return type; } }
            public Audio.SampleBuffer Samples { get { return samples; } }

            public Buffer(ASIOBufferInfo Info, ASIOSampleType Type, int Count)
            {
                info = Info;
                type = Type;
                samples = new Audio.SampleBuffer(Count);
            }
        }

        private double sampleRate;
        public override double SampleRate { get { return sampleRate; } }

        private AsioObject instance;
        private Audio.Stream.SampleHandler callback;
        private Buffer[] input;
        private Buffer[] output;

        private int buffer;

        private void OnBufferSwitch(int Index, ASIOBool Direct)
        {
            Audio.SampleBuffer[] a = new Audio.SampleBuffer[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                a[i] = input[i].Samples;

                using(Audio.RawLock l = new Audio.RawLock(a[i], false, true))
                    ConvertSamples(
                        input[i].Info.buffers[Index],
                        input[i].Type, 
                        l,
                        l.Count);
            }
            
            Audio.SampleBuffer[] b = new Audio.SampleBuffer[output.Length];
            for (int i = 0; i < output.Length; ++i)
                b[i] = output[i].Samples;

            callback(buffer, a, b, sampleRate);

            for (int i = 0; i < output.Length; ++i)
            {
                using (Audio.RawLock l = new Audio.RawLock(b[i], true, false))
                    ConvertSamples(
                        l,
                        output[i].Info.buffers[Index],
                        output[i].Type,
                        l.Count);
            }
        }

        private void OnSampleRateChange(double SampleRate)
        {
            sampleRate = SampleRate;
        }

        public Stream(AsioObject Instance, Audio.Stream.SampleHandler Callback, Channel[] Input, Channel[] Output)
            : base(Input, Output)
        {
            instance = Instance;
            callback = Callback;

            buffer = instance.BufferSize.Preferred;
            ASIOBufferInfo[] infos = new ASIOBufferInfo[Input.Length + Output.Length];
            for (int i = 0; i < Input.Length; ++i)
            {
                infos[i].isInput = ASIOBool.True;
                infos[i].channelNum = Input[i].Index;
            }
            for (int i = 0; i < Output.Length; ++i)
            {
                infos[Output.Length + i].isInput = ASIOBool.False;
                infos[Output.Length + i].channelNum = Output[i].Index;
            }

            instance.CreateBuffers(
                infos,
                buffer, 
                new ASIOCallbacks() { bufferSwitch = OnBufferSwitch, sampleRateDidChange = OnSampleRateChange });

            input = new Buffer[Input.Length];
            for (int i = 0; i < Input.Length; ++i)
                input[i] = new Buffer(infos[i], Input[i].Type, buffer);
            output = new Buffer[Output.Length];
            for (int i = 0; i < Output.Length; ++i)
                output[i] = new Buffer(infos[i], Output[i].Type, buffer);

            sampleRate = instance.SampleRate;
            instance.Start();
        }

        public override void Stop()
        {
            instance.Stop();
            instance.DisposeBuffers();
        }

        private static void ConvertSamples(IntPtr In, IntPtr Out, ASIOSampleType Type, int Count)
        {
            switch (Type)
            {
                //case ASIOSampleType.Int16MSB:
                //case ASIOSampleType.Int24MSB:
                //case ASIOSampleType.Int32MSB:
                //case ASIOSampleType.Float32MSB:
                //case ASIOSampleType.Float64MSB:
                //case ASIOSampleType.Int32MSB16:
                //case ASIOSampleType.Int32MSB18:
                //case ASIOSampleType.Int32MSB20:
                //case ASIOSampleType.Int32MSB24:
                case ASIOSampleType.Int16LSB: Audio.Util.LEf64ToLEi16(In, Out, Count); break;
                //case ASIOSampleType.Int24LSB:
                case ASIOSampleType.Int32LSB: Audio.Util.LEf64ToLEi32(In, Out, Count); break;
                case ASIOSampleType.Float32LSB: Audio.Util.LEf64ToLEf32(In, Out, Count); break;
                case ASIOSampleType.Float64LSB: Audio.Util.CopyMemory(Out, In, Count * sizeof(double)); break;
                //case ASIOSampleType.Int32LSB16:
                //case ASIOSampleType.Int32LSB18:
                //case ASIOSampleType.Int32LSB20:
                //case ASIOSampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }

        private static void ConvertSamples(IntPtr In, ASIOSampleType Type, IntPtr Out, int Count)
        {
            switch (Type)
            {
                //case ASIOSampleType.Int16MSB:
                //case ASIOSampleType.Int24MSB:
                //case ASIOSampleType.Int32MSB:
                //case ASIOSampleType.Float32MSB:
                //case ASIOSampleType.Float64MSB:
                //case ASIOSampleType.Int32MSB16:
                //case ASIOSampleType.Int32MSB18:
                //case ASIOSampleType.Int32MSB20:
                //case ASIOSampleType.Int32MSB24:
                case ASIOSampleType.Int16LSB: Audio.Util.LEi16ToLEf64(In, Out, Count); break;
                //case ASIOSampleType.Int24LSB:
                case ASIOSampleType.Int32LSB: Audio.Util.LEi32ToLEf64(In, Out, Count); break;
                case ASIOSampleType.Float32LSB: Audio.Util.LEf32ToLEf64(In, Out, Count); break;
                case ASIOSampleType.Float64LSB: Audio.Util.CopyMemory(Out, In, Count * sizeof(double)); break;
                //case ASIOSampleType.Int32LSB16:
                //case ASIOSampleType.Int32LSB18:
                //case ASIOSampleType.Int32LSB20:
                //case ASIOSampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }
    }
}
