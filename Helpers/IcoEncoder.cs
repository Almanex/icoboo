using System;
using System.Collections.Generic;
using System.IO;

namespace IconForge.Helpers
{
    public static class IcoEncoder
    {
        public struct IcoFrame
        {
            public byte[] PngData { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public static void Encode(IEnumerable<IcoFrame> frames, Stream outputStream)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

            using var writer = new BinaryWriter(outputStream);
            var frameList = new List<IcoFrame>(frames);

            // Write ICONDIR header
            writer.Write((ushort)0); // Reserved
            writer.Write((ushort)1); // Type (1 = Icon)
            writer.Write((ushort)frameList.Count); // Count of images

            // Calculate starting offset for the image data
            // Header is 6 bytes. Each entry is 16 bytes.
            int currentOffset = 6 + (frameList.Count * 16);

            // Write ICONDIRENTRY for each frame
            foreach (var frame in frameList)
            {
                byte widthByte = (byte)(frame.Width >= 256 ? 0 : frame.Width);
                byte heightByte = (byte)(frame.Height >= 256 ? 0 : frame.Height);

                writer.Write(widthByte);
                writer.Write(heightByte);
                writer.Write((byte)0); // Color count (0 for 256+ colors or PNGs)
                writer.Write((byte)0); // Reserved
                writer.Write((ushort)1); // Color Planes
                writer.Write((ushort)32); // Bits per pixel (PNG icons are standard 32bpp)
                writer.Write((uint)frame.PngData.Length); // Size of image data in bytes
                writer.Write((uint)currentOffset); // Offset of image data from start of file

                currentOffset += frame.PngData.Length;
            }

            // Write the actual PNG bytes
            foreach (var frame in frameList)
            {
                writer.Write(frame.PngData);
            }
        }
    }
}
