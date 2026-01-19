using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid.LowLevelRenderer.Core;

namespace Veldrid.ImageSharp
{
    public class ImageSharpTexture
    {
        /// <summary>
        /// An array of images, each a single element in the mipmap chain.
        /// The first element is the largest, most detailed level, and each subsequent element
        /// is half its size, down to 1x1 pixel.
        /// </summary>
        public Image<Rgba32>[] Images { get; }

        /// <summary>
        /// The width of the largest image in the chain.
        /// </summary>
        public uint Width => (uint)Images[0].Width;

        /// <summary>
        /// The height of the largest image in the chain.
        /// </summary>
        public uint Height => (uint)Images[0].Height;

        /// <summary>
        /// The pixel format of all images.
        /// </summary>
        public PixelFormat Format { get; }

        /// <summary>
        /// The size of each pixel, in bytes.
        /// </summary>
        public uint PixelSizeInBytes => sizeof(byte) * 4;

        /// <summary>
        /// The number of levels in the mipmap chain. This is equal to the length of the Images array.
        /// </summary>
        public uint MipLevels => (uint)Images.Length;

        public ImageSharpTexture(string path) : this(CreateDefaultImage(path), true) { }
        public ImageSharpTexture(string path, bool mipmap) : this(CreateDefaultImage(path), mipmap) { }
        public ImageSharpTexture(string path, bool mipmap, bool srgb) : this(CreateDefaultImage(path), mipmap, srgb) { }
        public ImageSharpTexture(Stream stream) : this(CreateDefaultImage(stream), true) { }
        public ImageSharpTexture(Stream stream, bool mipmap) : this(CreateDefaultImage(stream), mipmap) { }
        public ImageSharpTexture(Stream stream, bool mipmap, bool srgb) : this(CreateDefaultImage(stream), mipmap, srgb) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap = true) : this(image, mipmap, false) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap, bool srgb)
        {
            Format = srgb ? PixelFormat.R8_G8_B8_A8_UNorm_SRgb : PixelFormat.R8_G8_B8_A8_UNorm;
            if (mipmap)
            {
                Images = MipmapHelper.GenerateMipmaps(image);
            }
            else
            {
                Images = new Image<Rgba32>[] { image };
            }
        }

        public unsafe Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory)
        {
            return CreateTextureViaUpdate(gd, factory);
        }

        private unsafe Texture CreateTextureViaStaging(GraphicsDevice gd, ResourceFactory factory)
        {
            Texture staging = factory.CreateTexture(TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Staging));

            Texture ret = factory.CreateTexture(TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Sampled));

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            cl.Begin();

            for (uint level = 0; level < MipLevels; level++)
            {
                Image<Rgba32> image = Images[(int)level];

                int w = image.Width;
                int h = image.Height;

                uint rowWidth = (uint)(w * 4);           // RGBA32
                uint totalBytes = (uint)(w * h * 4);

                // Try fast path: contiguous pixel memory
                if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> mem))
                {
                    fixed (Rgba32* pin = &MemoryMarshal.GetReference(mem.Span))
                    {
                        WriteToStagingAndCopy(gd, staging, ret, cl, level, (uint)w, (uint)h, pin, rowWidth, totalBytes);
                    }
                }
                else
                {
                    Rgba32[] tmp = ArrayPool<Rgba32>.Shared.Rent(w * h);

                    image.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < h; y++)
                        {
                            accessor.GetRowSpan(y).CopyTo(tmp.AsSpan(y * w, w));
                        }
                    });

                    fixed (Rgba32* pin = &tmp[0])
                    {
                        WriteToStagingAndCopy(gd, staging, ret, cl, level, (uint)w, (uint)h, pin, rowWidth, totalBytes);
                    }
                }
            }

            cl.End();
            gd.SubmitCommands(cl);

            staging.Dispose();
            cl.Dispose();

            return ret;
        }

        private static unsafe void WriteToStagingAndCopy(GraphicsDevice gd, Texture staging, Texture ret, CommandList cl, uint level, uint width, uint height, Rgba32* srcPin, uint rowWidth, uint totalBytes)
        {
            MappedResource map = gd.Map(staging, MapMode.Write, level);

            if (rowWidth == map.RowPitch)
            {
                Unsafe.CopyBlock(map.Data.ToPointer(), srcPin, totalBytes);
            }
            else
            {
                for (uint y = 0; y < height; y++)
                {
                    byte* dstStart = (byte*)map.Data.ToPointer() + y * map.RowPitch;
                    byte* srcStart = (byte*)srcPin + y * rowWidth;
                    Unsafe.CopyBlock(dstStart, srcStart, rowWidth);
                }
            }

            gd.Unmap(staging, level);

            cl.CopyTexture(
                staging, 0, 0, 0, level, 0,
                ret, 0, 0, 0, level, 0,
                width, height, 1, 1);
        }

        private unsafe Texture CreateTextureViaUpdate(GraphicsDevice gd, ResourceFactory factory)
        {
            Texture tex = factory.CreateTexture(TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Sampled));

            for (int level = 0; level < MipLevels; level++)
            {
                Image<Rgba32> image = Images[level];

                int w = image.Width;
                int h = image.Height;
                uint sizeInBytes = (uint)(PixelSizeInBytes * w * h);

                if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> mem))
                {
                    fixed (Rgba32* pin = &MemoryMarshal.GetReference(mem.Span))
                    {
                        gd.UpdateTexture(tex, (IntPtr)pin, sizeInBytes, 0, 0, 0, (uint)w, (uint)h, 1, (uint)level, 0);
                    }
                }
                else
                {
                    Rgba32[] tmp = ArrayPool<Rgba32>.Shared.Rent(w * h);

                    image.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < h; y++)
                        {
                            Span<Rgba32> row = accessor.GetRowSpan(y);
                            row.CopyTo(tmp.AsSpan(y * w, w));
                        }
                    });

                    fixed (Rgba32* pin = &tmp[0])
                    {
                        gd.UpdateTexture(tex, (IntPtr)pin, sizeInBytes, 0, 0, 0, (uint)w, (uint)h, 1, (uint)level, 0);
                    }
                }
            }

            return tex;
        }

        private static Image<Rgba32> CreateDefaultImage(string path)
        {
            Configuration config = Configuration.Default.Clone();
            config.PreferContiguousImageBuffers = true;

            DecoderOptions options = new()
            {
                Configuration = config
            };

            return Image.Load<Rgba32>(options, path);
        }

        private static Image<Rgba32> CreateDefaultImage(Stream stream)
        {
            Configuration config = Configuration.Default.Clone();
            config.PreferContiguousImageBuffers = true;

            DecoderOptions options = new()
            {
                Configuration = config
            };

            return Image.Load<Rgba32>(options, stream);
        }
    }
}
