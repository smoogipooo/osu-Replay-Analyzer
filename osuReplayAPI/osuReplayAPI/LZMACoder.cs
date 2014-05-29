﻿using System;
using System.IO;
using SevenZip;

namespace ReplayAPI
{
    class LZMACoder
    {
        public MemoryStream Compress(MemoryStream inStream)
        {
            inStream.Position = 0;

            Int32 posStateBits = 2;
            Int32 litContextBits = 3;
            Int32 litPosBits = 0;
            Int32 algorithm = 2;

            bool eos = true;

            CoderPropID[] propIDs =  {
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.EndMarker
            };

            object[] properties = {
                (Int32)(posStateBits),
                (Int32)(litContextBits),
                (Int32)(litPosBits),
                (Int32)(algorithm),
                eos
            };

            var outStream = new MemoryStream();
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((Byte)(inStream.Length >> (8 * i)));
            encoder.Code(inStream, outStream, -1, -1, null);
            outStream.Flush();
            outStream.Position = 0;

            return outStream;
        }

        public MemoryStream Decompress(FileStream inStream)
        {
            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

            byte[] properties = new byte[5];
            if (inStream.Read(properties, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));
            decoder.SetDecoderProperties(properties);

            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = inStream.ReadByte();
                if (v < 0)
                    break;
                outSize |= ((long)(byte)v) << (8 * i);
            }
            long compressedSize = inStream.Length - inStream.Position;

            var outStream = new MemoryStream();
            decoder.Code(inStream, outStream, compressedSize, outSize, null);
            outStream.Flush();
            outStream.Position = 0;
            return outStream;
        }
    }
}