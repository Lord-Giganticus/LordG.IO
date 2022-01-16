using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace LordG.IO
{
    public class BTI : IDisposable
    {
        #region Sub Types
        public enum TextureFormats
        {
            I4 = 0x00,
            I8 = 0x01,
            IA4 = 0x02,
            IA8 = 0x03,
            RGB565 = 0x04,
            RGB5A3 = 0x05,
            RGBA32 = 0x06,
            C4 = 0x08,
            C8 = 0x09,
            C14X2 = 0x0a,
            CMPR = 0x0e,
        }

        public enum WrapModes
        {
            ClampToEdge = 0,
            Repeat = 1,
            MirroredRepeat = 2,
        }

        public enum PaletteFormats
        {
            IA8 = 0x00,
            RGB565 = 0x01,
            RGB5A3 = 0x02,
        }

        public enum FilterMode
        {
            Nearest = 0x0,
            Linear = 0x1,
            NearestMipmapNearest = 0x2,
            NearestMipmapLinear = 0x3,
            LinearMipmapNearest = 0x4,
            LinearMipmapLinear = 0x5, 
        }

        private sealed class Palette
        {
            public byte[] Data;

            public Palette(EndianReader reader, uint count)
            {
                if (count is 0)
                {
                    Data = Array.Empty<byte>();
                    return;
                }
                Data = reader.ReadBytes((int)count * 2);
            }
        }
        #endregion

        public string Name;

        public TextureFormats Format;

        public byte AlphaSetting;

        public ushort Width;

        public ushort Height;

        public WrapModes WrapS;

        public WrapModes WrapT;

        public bool PalettesEnabled;

        public PaletteFormats PaletteFormat;

        public ushort PaletteCount;

        public int EmbeddedPaletteOffset;

        public FilterMode MinFilter;

        public FilterMode MagFilter;

        public sbyte MinLOD;

        public sbyte MagLOD;

        public byte MipMapCount;

        public short LodBias;

        private Palette m_imagePalette;

        public Image<Rgba32> Data;

        public short unknown2 = 0;
        public byte unknown3 = 0;

        public void Dispose()
        {
            Data.Dispose();
        }

        public BTI()
        {
            MipMapCount = 1;
        }

        public BTI(string name)
        {
            Name = name;
        }

        public BTI(EndianReader reader)
        {
            Format = (TextureFormats)reader.ReadByte();
            AlphaSetting = reader.ReadByte();
            Width = reader.ReadNumeric<ushort>();
            Height = reader.ReadNumeric<ushort>();
            WrapS = (WrapModes)reader.ReadByte();
            WrapT = (WrapModes)reader.ReadByte();
            PalettesEnabled = reader.ReadBoolean();
            PaletteFormat = (PaletteFormats)reader.ReadByte();
            PaletteCount = reader.ReadNumeric<ushort>();
            int palletdataoffset = reader.ReadNumeric<int>();
            EmbeddedPaletteOffset = reader.ReadNumeric<int>();
            MinFilter = (FilterMode)reader.ReadByte();
            MagFilter = (FilterMode)reader.ReadByte();
            unknown2 = reader.ReadNumeric<short>();
            MipMapCount = reader.ReadByte();
            unknown3 = reader.ReadByte();
            LodBias = reader.ReadNumeric<short>();
            int imagedataoffdet = reader.ReadNumeric<int>();
            reader.SeekBegin(palletdataoffset + 0x20);
            m_imagePalette = new Palette(reader, PaletteCount);
            reader.SeekBegin(imagedataoffdet + 0x20);
            Data = Image.LoadPixelData<Rgba32>(DecodeData(reader), Width, Height);   
        }

        private byte[] DecodeData(EndianReader reader)
        {
            switch (Format)
            {
                case TextureFormats.I4:
                    return DecodeI4(reader);
                case TextureFormats.I8:
                    return DecodeI8(reader);
                case TextureFormats.IA4:
                    return DecodeIA4(reader);
                case TextureFormats.IA8:
                    return DecodeIA8(reader);
                case TextureFormats.RGB565:
                    return DecodeRgb565(reader);
                case TextureFormats.RGB5A3:
                    return DecodeRgb5A3(reader);
                case TextureFormats.RGBA32:
                    return DecodeRgba32(reader);
                case TextureFormats.C4:
                    return DecodeC4(reader);
                case TextureFormats.C8:
                    return DecodeC8(reader);
                case TextureFormats.CMPR:
                    return DecodeCmpr(reader);
                default:
                    throw new Exception("BTI does not contain supported Texure Format.");
            }
        }

        private byte[] DecodeI4(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 7) / 8;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 8; pY++)
                    {
                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            if ((xBlock * 8 + pX >= Width) || (yBlock * 8 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                continue;
                            }
                            byte data = reader.ReadByte();
                            byte t = (byte)((data & 0xF0) >> 4);
                            byte t2 = (byte)(data & 0x0F);
                            uint destIndex = (uint)(4 * (Width * ((yBlock * 8) + pY) + (xBlock * 8) + pX));
                            decodedData[destIndex + 0] = (byte)(t * 0x11);
                            decodedData[destIndex + 1] = (byte)(t * 0x11);
                            decodedData[destIndex + 2] = (byte)(t * 0x11);
                            decodedData[destIndex + 3] = (byte)(t * 0x11);

                            decodedData[destIndex + 4] = (byte)(t2 * 0x11);
                            decodedData[destIndex + 5] = (byte)(t2 * 0x11);
                            decodedData[destIndex + 6] = (byte)(t2 * 0x11);
                            decodedData[destIndex + 7] = (byte)(t2 * 0x11);
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeI8(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            if ((xBlock * 8 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                continue;
                            }
                            byte data = reader.ReadByte();
                            uint destIndex = (uint)(4 * (Width * ((yBlock * 4) + pY) + (xBlock * 8) + pX));
                            decodedData[destIndex + 0] = data;
                            decodedData[destIndex + 1] = data;
                            decodedData[destIndex + 2] = data;
                            decodedData[destIndex + 3] = data;
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeIA4(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            if ((xBlock * 8 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                continue;
                            }
                            byte value = reader.ReadByte();

                            byte alpha = (byte)((value & 0xF0) >> 4);
                            byte lum = (byte)(value & 0x0F);

                            uint destIndex = (uint)(4 * (Width * ((yBlock * 4) + pY) + (xBlock * 8) + pX));

                            decodedData[destIndex + 0] = (byte)(lum * 0x11);
                            decodedData[destIndex + 1] = (byte)(lum * 0x11);
                            decodedData[destIndex + 2] = (byte)(lum * 0x11);
                            decodedData[destIndex + 3] = (byte)(alpha * 0x11);
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeIA8(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 3) / 4;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            if ((xBlock * 4 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                reader.BaseStream.Position++;
                                continue;
                            }
                            uint destIndex = (uint)(4 * (Width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                            byte byte0 = reader.ReadByte();
                            byte byte1 = reader.ReadByte();
                            decodedData[destIndex + 3] = byte0;
                            decodedData[destIndex + 2] = byte1;
                            decodedData[destIndex + 1] = byte1;
                            decodedData[destIndex + 0] = byte1;
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeRgb565(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 3) / 4;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            if ((xBlock * 4 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                continue;
                            }
                            ushort sourcePixel = reader.ReadNumeric<ushort>();
                            RGB565ToRGBA8(sourcePixel, ref decodedData, 4 * (Width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeRgb5A3(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 3) / 4;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            if ((xBlock * 4 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position += sizeof(ushort);
                                continue;
                            }
                            ushort sourcePixel = reader.ReadNumeric<ushort>();
                            RGB5A3ToRGBA8(sourcePixel, ref decodedData, 4 * (Width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeRgba32(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 3) / 4;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            if ((xBlock * 4 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                reader.BaseStream.Position++;
                                continue;
                            }
                            uint destIndex = (uint)(4 * (Width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                            decodedData[destIndex + 3] = reader.ReadByte();
                            decodedData[destIndex + 2] = reader.ReadByte();
                        }
                    }
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 4; pX++)
                        {
                            if ((xBlock * 4 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                reader.BaseStream.Position++;
                                continue;
                            }
                            uint destIndex = (uint)(4 * (Width * ((yBlock * 4) + pY) + (xBlock * 4) + pX));
                            decodedData[destIndex + 1] = reader.ReadByte(); //Green
                            decodedData[destIndex + 0] = reader.ReadByte();
                        }
                    }
                }
            }
            return decodedData;
        }

        private byte[] DecodeC4(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 7) / 8;
            byte[] decodedData = new byte[Width * Height * 8];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 8; pY++)
                    {
                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            if ((xBlock * 8 + pX >= Width) || (yBlock * 8 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                continue;
                            }
                            byte data = reader.ReadByte();
                            byte t = (byte)(data & 0xF0);
                            byte t2 = (byte)(data & 0x0F);
                            decodedData[Width * ((yBlock * 8) + pY) + (xBlock * 8) + pX + 0] = (byte)(t >> 4);
                            decodedData[Width * ((yBlock * 8) + pY) + (xBlock * 8) + pX + 1] = t2;
                        }
                    }
                }
            }
            byte[] finalDest = new byte[decodedData.Length / 2];

            int pixelSize = PaletteFormat == PaletteFormats.IA8 ? 2 : 4;
            int destOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    UnpackPixelFromPalette(decodedData[y * Width + x], ref finalDest, destOffset, m_imagePalette.Data, PaletteFormat);
                    destOffset += pixelSize;
                }
            }
            return finalDest;
        }

        private byte[] DecodeC8(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 3) / 4;
            byte[] decodedData = new byte[Width * Height * 8];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            if ((xBlock * 8 + pX >= Width) || (yBlock * 4 + pY >= Height))
                            {
                                reader.BaseStream.Position++;
                                continue;
                            }
                            byte data = reader.ReadByte();
                            decodedData[Width * ((yBlock * 4) + pY) + (xBlock * 8) + pX] = data;
                        }
                    }
                }
            }
            byte[] finalDest = new byte[decodedData.Length / 2];
            int pixelSize = PaletteFormat == PaletteFormats.IA8 ? 2 : 4;
            int destOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    UnpackPixelFromPalette(decodedData[y * Width + x], ref finalDest, destOffset, m_imagePalette.Data, PaletteFormat);
                    destOffset += pixelSize;
                }
            }
            return finalDest;
        }

        private byte[] DecodeCmpr(EndianReader reader)
        {
            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 7) / 8;
            byte[] decodedData = new byte[Width * Height * 4];
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int ySubBlock = 0; ySubBlock < 2; ySubBlock++)
                    {
                        for (int xSubBlock = 0; xSubBlock < 2; xSubBlock++)
                        {
                            int subBlockWidth = Math.Max(0, Math.Min(4, Width - (xSubBlock * 4 + xBlock * 8)));
                            int subBlockHeight = Math.Max(0, Math.Min(4, Height - (ySubBlock * 4 + yBlock * 8)));
                            byte[] subBlockData = DecodeCmprSubBlock(reader);
                            for (int pY = 0; pY < subBlockHeight; pY++)
                            {
                                int destX = xBlock * 8 + xSubBlock * 4;
                                int destY = yBlock * 8 + ySubBlock * 4 + pY;
                                if (destX >= Width || destY >= Height)
                                    continue;
                                int destOffset = (destY * Width + destX) * 4;
                                Buffer.BlockCopy(subBlockData, (int)(pY * 4 * 4), decodedData, destOffset, (int)(subBlockWidth * 4));
                            }
                        }
                    }
                }
            }
            return decodedData;
        }

        private static void RGB565ToRGBA8(ushort sourcePixel, ref byte[] dest, int destOffset)
        {
            byte r, g, b;
            r = (byte)((sourcePixel & 0xF800) >> 11);
            g = (byte)((sourcePixel & 0x7E0) >> 5);
            b = (byte)((sourcePixel & 0x1F));

            r = (byte)((r << (8 - 5)) | (r >> (10 - 8)));
            g = (byte)((g << (8 - 6)) | (g >> (12 - 8)));
            b = (byte)((b << (8 - 5)) | (b >> (10 - 8)));

            dest[destOffset] = b;
            dest[destOffset + 1] = g;
            dest[destOffset + 2] = r;
            dest[destOffset + 3] = 0xFF; //Set alpha to 1
        }

        private static void RGB5A3ToRGBA8(ushort sourcePixel, ref byte[] dest, int destOffset)
        {
            byte r, g, b, a;
            if ((sourcePixel & 0x8000) == 0x8000)
            {
                a = 0xFF;
                r = (byte)((sourcePixel & 0x7C00) >> 10);
                g = (byte)((sourcePixel & 0x3E0) >> 5);
                b = (byte)(sourcePixel & 0x1F);

                r = (byte)((r << (8 - 5)) | (r >> (10 - 8)));
                g = (byte)((g << (8 - 5)) | (g >> (10 - 8)));
                b = (byte)((b << (8 - 5)) | (b >> (10 - 8)));
            }
            else
            {
                a = (byte)((sourcePixel & 0x7000) >> 12);
                r = (byte)((sourcePixel & 0xF00) >> 8);
                g = (byte)((sourcePixel & 0xF0) >> 4);
                b = (byte)(sourcePixel & 0xF);

                a = (byte)((a << (8 - 3)) | (a << (8 - 6)) | (a >> (9 - 8)));
                r = (byte)((r << (8 - 4)) | r);
                g = (byte)((g << (8 - 4)) | g);
                b = (byte)((b << (8 - 4)) | b);
            }

            dest[destOffset + 0] = b;
            dest[destOffset + 1] = g;
            dest[destOffset + 2] = r;
            dest[destOffset + 3] = a;
        }

        private static void UnpackPixelFromPalette(int paletteIndex, ref byte[] dest, int offset, byte[] paletteData, PaletteFormats format)
        {
            switch (format)
            {
                case PaletteFormats.IA8:
                    dest[0] = paletteData[2 * paletteIndex + 1];
                    dest[1] = paletteData[2 * paletteIndex + 0];
                    break;
                case PaletteFormats.RGB565:
                    {
                        ushort palettePixelData = (ushort)((Buffer.GetByte(paletteData, 2 * paletteIndex) << 8) | Buffer.GetByte(paletteData, 2 * paletteIndex + 1));
                        RGB565ToRGBA8(palettePixelData, ref dest, offset);
                    }
                    break;
                case PaletteFormats.RGB5A3:
                    {
                        ushort palettePixelData = (ushort)((Buffer.GetByte(paletteData, 2 * paletteIndex) << 8) | Buffer.GetByte(paletteData, 2 * paletteIndex + 1));
                        RGB5A3ToRGBA8(palettePixelData, ref dest, offset);
                    }
                    break;
            }
        }

        private static byte[] DecodeCmprSubBlock(EndianReader reader)
        {
            byte[] decodedData = new byte[4 * 4 * 4];
            ushort color1 = reader.ReadNumeric<ushort>();
            ushort color2 = reader.ReadNumeric<ushort>();
            uint bits = reader.ReadNumeric<uint>();
            byte[][] ColorTable = new byte[4][];
            for (int i = 0; i < 4; i++)
                ColorTable[i] = new byte[4];
            RGB565ToRGBA8(color1, ref ColorTable[0], 0);
            RGB565ToRGBA8(color2, ref ColorTable[1], 0);
            if (color1 > color2)
            {
                ColorTable[2][0] = (byte)((2 * ColorTable[0][0] + ColorTable[1][0]) / 3);
                ColorTable[2][1] = (byte)((2 * ColorTable[0][1] + ColorTable[1][1]) / 3);
                ColorTable[2][2] = (byte)((2 * ColorTable[0][2] + ColorTable[1][2]) / 3);
                ColorTable[2][3] = 0xFF;

                ColorTable[3][0] = (byte)((ColorTable[0][0] + 2 * ColorTable[1][0]) / 3);
                ColorTable[3][1] = (byte)((ColorTable[0][1] + 2 * ColorTable[1][1]) / 3);
                ColorTable[3][2] = (byte)((ColorTable[0][2] + 2 * ColorTable[1][2]) / 3);
                ColorTable[3][3] = 0xFF;
            }
            else
            {
                ColorTable[2][0] = (byte)((ColorTable[0][0] + ColorTable[1][0]) / 2);
                ColorTable[2][1] = (byte)((ColorTable[0][1] + ColorTable[1][1]) / 2);
                ColorTable[2][2] = (byte)((ColorTable[0][2] + ColorTable[1][2]) / 2);
                ColorTable[2][3] = 0xFF;

                ColorTable[3][0] = (byte)((ColorTable[0][0] + 2 * ColorTable[1][0]) / 3);
                ColorTable[3][1] = (byte)((ColorTable[0][1] + 2 * ColorTable[1][1]) / 3);
                ColorTable[3][2] = (byte)((ColorTable[0][2] + 2 * ColorTable[1][2]) / 3);
                ColorTable[3][3] = 0x00;
            }

            for (int iy = 0; iy < 4; ++iy)
            {
                for (int ix = 0; ix < 4; ++ix)
                {
                    int i = iy * 4 + ix;
                    int bitOffset = (15 - i) * 2;
                    int di = i * 4;
                    int si = (int)((bits >> bitOffset) & 0x3);
                    decodedData[di + 0] = ColorTable[si][0];
                    decodedData[di + 1] = ColorTable[si][1];
                    decodedData[di + 2] = ColorTable[si][2];
                    decodedData[di + 3] = ColorTable[si][3];
                }
            }
            return decodedData;
        }
    }
}
