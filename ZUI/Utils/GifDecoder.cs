using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZUI.Utils
{
    public class GifFrame
    {
        public Texture2D Texture;
        public float Delay; // In seconds
    }

    public static class GifDecoder
    {
        public static List<GifFrame> Decode(byte[] fileData)
        {
            List<GifFrame> frames = new List<GifFrame>();
            if (fileData == null || fileData.Length == 0) return frames;

            using (var stream = new MemoryStream(fileData))
            using (var reader = new BinaryReader(stream))
            {
                // 1. Header
                string signature = new string(reader.ReadChars(3));
                string version = new string(reader.ReadChars(3));

                if (signature != "GIF")
                {
                    LogUtils.LogError("Invalid GIF signature.");
                    return frames;
                }

                // 2. Logical Screen Descriptor
                ushort screenWidth = reader.ReadUInt16();
                ushort screenHeight = reader.ReadUInt16();
                byte packedFields = reader.ReadByte();
                byte bgColorIndex = reader.ReadByte();
                byte pixelAspectRatio = reader.ReadByte();

                bool globalColorTableFlag = (packedFields & 0x80) != 0;
                int globalColorTableSize = 2 << (packedFields & 0x07);

                // 3. Global Color Table
                Color32[] globalColorTable = null;
                if (globalColorTableFlag)
                {
                    globalColorTable = ReadColorTable(reader, globalColorTableSize);
                }

                // State variables for processing frames
                Color32[] currentFramePixels = new Color32[screenWidth * screenHeight];
                // Fill with transparent/bg initially
                for (int i = 0; i < currentFramePixels.Length; i++) currentFramePixels[i] = new Color32(0, 0, 0, 0);

                Color32[] lastFramePixels = null; // For disposal method

                float currentDelay = 0.1f;
                int transparentColorIndex = -1;
                int disposalMethod = 0; // 0=NoAction, 1=DoNotDispose, 2=RestoreBg, 3=RestorePrev

                // 4. Blocks
                while (stream.Position < stream.Length)
                {
                    byte blockType = reader.ReadByte();

                    if (blockType == 0x2C) // Image Descriptor
                    {
                        ushort left = reader.ReadUInt16();
                        ushort top = reader.ReadUInt16();
                        ushort width = reader.ReadUInt16();
                        ushort height = reader.ReadUInt16();
                        byte imgPacked = reader.ReadByte();

                        bool localColorTableFlag = (imgPacked & 0x80) != 0;
                        bool interlaceFlag = (imgPacked & 0x40) != 0;
                        int localColorTableSize = 2 << (imgPacked & 0x07);

                        Color32[] activeColorTable = globalColorTable;
                        if (localColorTableFlag)
                        {
                            activeColorTable = ReadColorTable(reader, localColorTableSize);
                        }

                        // Read Image Data (LZW)
                        byte lzwMinCodeSize = reader.ReadByte();
                        byte[] imageData = ReadSubBlocks(reader);

                        // Decompress
                        byte[] indices = DecompressLZW(imageData, lzwMinCodeSize, width * height);

                        // Create Texture
                        // We need to apply these indices onto the currentFramePixels based on disposal method

                        // Handle Disposal of previous frame before drawing new one
                        if (disposalMethod == 2) // Restore to BG
                        {
                            // Simplified: Clear to transparent
                            // In a full renderer we'd track rects, but for UI icons this usually suffices
                            // or we rely on the paint loop below to overwrite.
                            // A strict decoder would revert specific pixels.
                        }
                        else if (disposalMethod == 3 && lastFramePixels != null)
                        {
                            Array.Copy(lastFramePixels, currentFramePixels, currentFramePixels.Length);
                        }

                        // Save current state for next disposal if needed
                        lastFramePixels = (Color32[])currentFramePixels.Clone();

                        // Paint new pixels
                        int pixelIdx = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (pixelIdx >= indices.Length) break;

                                byte index = indices[pixelIdx++];

                                // Handle transparency
                                if (transparentColorIndex > -1 && index == transparentColorIndex)
                                {
                                    continue; // Skip, leave existing pixel
                                }

                                if (activeColorTable != null && index < activeColorTable.Length)
                                {
                                    // GIF coords (top-left) vs Unity coords (bottom-left)
                                    // We'll flip Y at the end or draw inverted.
                                    // Let's write to the flat array assuming Row-Major Top-Left.

                                    int targetX = left + x;
                                    int targetY = top + y; // Top-down

                                    // Safety
                                    if (targetX < screenWidth && targetY < screenHeight)
                                    {
                                        // Invert Y for Unity texture (Bottom-Up)
                                        int unityIndex = (screenHeight - 1 - targetY) * screenWidth + targetX;
                                        currentFramePixels[unityIndex] = activeColorTable[index];
                                    }
                                }
                            }
                        }

                        // Create Unity Texture
                        Texture2D frameTex = new Texture2D(screenWidth, screenHeight, TextureFormat.RGBA32, false);
                        frameTex.SetPixels32(currentFramePixels);
                        frameTex.Apply();
                        frameTex.wrapMode = TextureWrapMode.Clamp;
                        frameTex.filterMode = FilterMode.Point; // Pixel art style usually better for GIFs

                        frames.Add(new GifFrame { Texture = frameTex, Delay = currentDelay });

                        // Reset frame specific controls
                        transparentColorIndex = -1;
                        disposalMethod = 0;
                        currentDelay = 0.1f;
                    }
                    else if (blockType == 0x21) // Extension
                    {
                        byte extensionType = reader.ReadByte();
                        if (extensionType == 0xF9) // Graphic Control Extension
                        {
                            reader.ReadByte(); // Block Size (4)
                            byte packed = reader.ReadByte();
                            disposalMethod = (packed & 0x1C) >> 2;
                            bool transparentFlag = (packed & 0x01) != 0;

                            ushort delay = reader.ReadUInt16(); // 1/100th seconds
                            currentDelay = delay / 100f;
                            if (currentDelay < 0.02f) currentDelay = 0.1f; // Fix bad headers

                            byte transIndex = reader.ReadByte();
                            if (transparentFlag) transparentColorIndex = transIndex;

                            reader.ReadByte(); // Terminator
                        }
                        else
                        {
                            // Skip other extensions (Application, Comment, etc)
                            ReadSubBlocks(reader);
                        }
                    }
                    else if (blockType == 0x3B) // Trailer
                    {
                        break;
                    }
                }
            }

            return frames;
        }

        private static Color32[] ReadColorTable(BinaryReader reader, int size)
        {
            Color32[] table = new Color32[size];
            for (int i = 0; i < size; i++)
            {
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                table[i] = new Color32(r, g, b, 255);
            }
            return table;
        }

        private static byte[] ReadSubBlocks(BinaryReader reader)
        {
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    byte blockSize = reader.ReadByte();
                    if (blockSize == 0) break;
                    byte[] data = reader.ReadBytes(blockSize);
                    ms.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        private static byte[] DecompressLZW(byte[] compressedData, int minCodeSize, int expectedPixelCount)
        {
            // Standard GIF LZW Decompression
            // Adapted for C# 
            List<byte> output = new List<byte>(expectedPixelCount);

            int clearCode = 1 << minCodeSize;
            int eoiCode = clearCode + 1;
            int nextCode = eoiCode + 1;
            int codeSize = minCodeSize + 1;
            int codeMask = (1 << codeSize) - 1;

            Dictionary<int, List<byte>> dictionary = InitializeDictionary(minCodeSize);

            int bitPointer = 0;
            int oldCode = -1;

            // Helper to get bits
            int GetCode()
            {
                int code = 0;
                for (int i = 0; i < codeSize; i++)
                {
                    int byteIndex = bitPointer / 8;
                    int bitIndex = bitPointer % 8;
                    if (byteIndex >= compressedData.Length) return eoiCode; // End of data safety

                    if ((compressedData[byteIndex] & (1 << bitIndex)) != 0)
                        code |= (1 << i);

                    bitPointer++;
                }
                return code;
            }

            while (true)
            {
                int code = GetCode();

                if (code == eoiCode) break;

                if (code == clearCode)
                {
                    codeSize = minCodeSize + 1;
                    codeMask = (1 << codeSize) - 1;
                    nextCode = eoiCode + 1;
                    dictionary = InitializeDictionary(minCodeSize);
                    oldCode = -1;
                    continue;
                }

                if (oldCode == -1)
                {
                    if (dictionary.ContainsKey(code))
                        output.AddRange(dictionary[code]);
                    oldCode = code;
                    continue;
                }

                List<byte> entry;
                if (dictionary.ContainsKey(code))
                {
                    entry = dictionary[code];
                    output.AddRange(entry);
                }
                else if (code == nextCode)
                {
                    entry = new List<byte>(dictionary[oldCode]);
                    entry.Add(entry[0]);
                    output.AddRange(entry);
                }
                else
                {
                    // Invalid code logic, usually corrupt data
                    continue;
                }

                // Add to dictionary
                if (nextCode < 4096)
                {
                    List<byte> newEntry = new List<byte>(dictionary[oldCode]);
                    newEntry.Add(entry[0]);
                    dictionary[nextCode++] = newEntry;

                    // Increase code size if needed
                    if (nextCode >= (1 << codeSize) && codeSize < 12)
                    {
                        codeSize++;
                        codeMask = (1 << codeSize) - 1;
                    }
                }

                oldCode = code;
            }

            return output.ToArray();
        }

        private static Dictionary<int, List<byte>> InitializeDictionary(int minCodeSize)
        {
            var dict = new Dictionary<int, List<byte>>();
            int count = 1 << minCodeSize;
            for (int i = 0; i < count; i++)
            {
                dict[i] = new List<byte> { (byte)i };
            }
            return dict;
        }
    }
}