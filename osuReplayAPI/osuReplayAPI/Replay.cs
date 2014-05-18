﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using smgiFuncs;

namespace ReplayAPI
{
    public class Replay
    {
        public GameModes GameMode;
        public string Filename;
        public int FileFormat;
        public string MapHash;
        public string PlayerName;
        public string ReplayHash;
        public int TotalScore;
        public int Count_300;
        public int Count_100;
        public int Count_50;
        public int Count_Geki;
        public int Count_Katu;
        public int Count_Miss;
        public int MaxCombo;
        public int IsPerfect;
        public Modifications Mods;
        public List<LifeInfo> LifeData = new List<LifeInfo>();
        public DateTime PlayTime;
        public int ReplayLength;
        public List<ReplayInfo> ReplayFrames = new List<ReplayInfo>();
        public List<ReplayInfo> ClickFrames = new List<ReplayInfo>();

        public Replay(string replayFile)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);
            Parse(replayFile);
        }

        public void Parse(string replayFile)
        {
            Filename = replayFile;
            using (FileStream fs = new FileStream(replayFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                GameMode = (GameModes)Enum.Parse(typeof(GameModes), br.ReadByte().ToString(CultureInfo.InvariantCulture));

                FileFormat = int.Parse(GetReversedString(br, 4), NumberStyles.HexNumber);

                br.ReadByte();
                MapHash = Encoding.ASCII.GetString(br.ReadBytes(GetChunkLength(br))); //Hash type: MD5

                br.ReadByte();
                PlayerName = Encoding.ASCII.GetString(br.ReadBytes(GetChunkLength(br)));

                br.ReadByte();
                ReplayHash = Encoding.ASCII.GetString(br.ReadBytes(GetChunkLength(br))); //Hash type: MD5

                Count_300 = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);
                Count_100 = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);
                Count_50 = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);
                Count_Geki = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);
                Count_Katu = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);
                Count_Miss = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);

                TotalScore = int.Parse(GetReversedString(br, 4), NumberStyles.HexNumber);

                MaxCombo = int.Parse(GetReversedString(br, 2), NumberStyles.HexNumber);

                IsPerfect = br.ReadByte();

                Mods = (Modifications)Enum.Parse(typeof(Modifications), int.Parse(GetReversedString(br, 4), NumberStyles.HexNumber).ToString(CultureInfo.InvariantCulture));

                bool lifeExists = int.Parse(GetReversedString(br, 1), NumberStyles.HexNumber) == 0x0B;
                if (lifeExists)
                {
                    string tempLifeStr = Encoding.ASCII.GetString(br.ReadBytes(GetChunkLength(br)));
                    foreach (string splitStr in Regex.Split(tempLifeStr, ","))
                    {
                        if (splitStr == "")
                            continue;
                        sString tempStr = splitStr;
                        LifeInfo tempLife = new LifeInfo
                        {
                            Time = Convert.ToInt32(tempStr.SubString(0, tempStr.nthDexOf("|", 0))),
                            Percentage = Convert.ToDouble(tempStr.SubString(tempStr.nthDexOf("|", 0) + 1)),
                        };
                        LifeData.Add(tempLife);
                    }
                }
                long timeStamp = long.Parse(GetReversedString(br, 8), NumberStyles.HexNumber);
                PlayTime = new DateTime(timeStamp);

                ReplayLength = int.Parse(GetReversedString(br, 4), NumberStyles.HexNumber);

                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] bytesToWrite = br.ReadBytes(ReplayLength + 1);
                    ms.Write(bytesToWrite, 0, bytesToWrite.Length);
                    ms.Position = 0;

                    byte[] properties = new byte[5];
                    if (ms.Read(properties, 0, 5) != 5) { }
                    SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
                    decoder.SetDecoderProperties(properties);
                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = ms.ReadByte();
                        if (v < 0)
                            break;
                        outSize |= ((long)(byte)v) << (8 * i);
                    }
                    long compressedSize = ms.Length - ms.Position;
                    MemoryStream outStream = new MemoryStream();
                    decoder.Code(ms, outStream, compressedSize, outSize, null);
                    outStream.Flush();
                    outStream.Position = 0;

                    string outString;
                    using (StreamReader reader = new StreamReader(outStream))
                    {
                        outString = reader.ReadToEnd();
                    }
                    int lastTime = 0;
                    KeyData lastKey = new KeyData();
                    foreach (string splitStr in Regex.Split(outString, ","))
                    {
                        if (splitStr == "")
                            continue;
                        sString tempStr = splitStr;
                        ReplayInfo tempInfo = new ReplayInfo();
                        tempInfo.TimeDiff = Convert.ToInt64(tempStr.SubString(0, tempStr.nthDexOf("|", 0)));
                        lastTime += (int)tempInfo.TimeDiff;
                        tempInfo.Time = lastTime;
                        tempInfo.X = Convert.ToDouble(tempStr.SubString(tempStr.nthDexOf("|", 0) + 1, tempStr.nthDexOf("|", 1)));
                        tempInfo.Y = Convert.ToDouble(tempStr.SubString(tempStr.nthDexOf("|", 1) + 1, tempStr.nthDexOf("|", 2)));
                        tempInfo.Keys = (KeyData)Enum.Parse(typeof(KeyData), tempStr.SubString(tempStr.nthDexOf("|", 2) + 1));
                        if (tempInfo.Keys != KeyData.None && lastKey != tempInfo.Keys)
                        {
                            ClickFrames.Add(tempInfo);
                        }
                        ReplayFrames.Add(tempInfo);
                        lastKey = tempInfo.Keys;
                    }
                }

                //Get a list of all the individual clicks
                for (int i = 0; i < ReplayFrames.Count; i++)
                {
                    if (ReplayFrames[i].Keys != KeyData.None)
                    {
                        ClickFrames.Add(ReplayFrames[i]);
                    }
                    for (int n = i; n < ReplayFrames.Count; n++)
                    {
                        if (ReplayFrames[n].Keys != ReplayFrames[i].Keys)
                        {
                            i = n;
                            break;
                        }
                    }
                }
            }
        }

        static string GetReversedString(BinaryReader br, int length)
        {
            byte[] readBytes = br.ReadBytes(length).Reverse().ToArray();
            return readBytes.Aggregate("", (current, b) => current + (b < 16 ? "0" : "") + b.ToString("X"));
        }
        static int GetChunkLength(BinaryReader br)
        {
            int shift = 0;
            int chunkLength = 0;
            while (true)
            {
                byte b = br.ReadByte();
                chunkLength |= (b & 127) << shift;
                if ((b & 128) == 0)
                    break;
                shift += 7;
            }
            return chunkLength;
        }

        public enum GameModes
        {
            osu = 0,
            Taiko = 1,
            CtB = 2,
            Mania = 3
        }
    }
}
