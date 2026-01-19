using System;
using System.Text;
using Veldrid.LowLevelRenderer.Core;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace Veldrid.Tests
{
    public static class TestUtils
    {
        private static readonly bool InitializedSdl2;
        private static readonly string InitializationFailedMessage;

        static unsafe TestUtils()
        {
            int result = Sdl2Native.SDL_Init(SDLInitFlags.Video);
            if (result != 0)
            {
                InitializationFailedMessage = GetString(Sdl2Native.SDL_GetError());
                Console.WriteLine($"Failed to initialize SDL2: {InitializationFailedMessage}");
                InitializedSdl2 = false;
            }
            else
            {
                InitializedSdl2 = true;
            }
        }

        public static GraphicsDevice CreateVulkanDevice()
        {
            return GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions(true));
        }

        public static void CreateVulkanDeviceWithSwapchain(out Sdl2Window window, out GraphicsDevice gd)
        {
            if (!InitializedSdl2)
            {
                window = null;
                gd = null;
                return;
            }

            WindowCreateInfo wci = new WindowCreateInfo
            {
                WindowWidth = 200,
                WindowHeight = 200,
                WindowInitialState = WindowState.Hidden,
            };

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(true, PixelFormat.R16_UNorm, false);

            VeldridStartup.CreateWindowAndGraphicsDevice(wci, options, GraphicsBackend.Vulkan, out window, out gd);
        }

        internal static unsafe string GetString(byte* stringStart)
        {
            int characters = 0;
            while (stringStart[characters] != 0)
            {
                characters++;
            }

            return Encoding.UTF8.GetString(stringStart, characters);
        }
    }

    public abstract class GraphicsDeviceTestBase<T> : IDisposable where T : GraphicsDeviceCreator
    {
        private readonly Sdl2Window _window;
        private readonly GraphicsDevice _gd;
        private readonly DisposeCollectorResourceFactory _factory;
        private readonly RenderDoc _renderDoc;

        public GraphicsDevice GD => _gd;
        public ResourceFactory RF => _factory;
        public Sdl2Window Window => _window;
        public RenderDoc RenderDoc => _renderDoc;

        public GraphicsDeviceTestBase()
        {
            if (Environment.GetEnvironmentVariable("VELDRID_TESTS_ENABLE_RENDERDOC") == "1"
                && RenderDoc.Load(out _renderDoc))
            {
                _renderDoc.APIValidation = true;
                _renderDoc.DebugOutputMute = false;
            }
            Activator.CreateInstance<T>().CreateGraphicsDevice(out _window, out _gd);
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
        }

        protected DeviceBuffer GetReadback(DeviceBuffer buffer)
        {
            DeviceBuffer readback;
            if ((buffer.Usage & BufferUsage.Staging) != 0)
            {
                readback = buffer;
            }
            else
            {
                readback = RF.CreateBuffer(new BufferDescription(buffer.SizeInBytes, BufferUsage.Staging));
                CommandList cl = RF.CreateCommandList();
                cl.Begin();
                cl.CopyBuffer(buffer, 0, readback, 0, buffer.SizeInBytes);
                cl.End();
                GD.SubmitCommands(cl);
                GD.WaitForIdle();
            }

            return readback;
        }

        protected Texture GetReadback(Texture texture)
        {
            if ((texture.Usage & TextureUsage.Staging) != 0)
            {
                return texture;
            }
            else
            {
                uint layers = texture.ArrayLayers;
                if ((texture.Usage & TextureUsage.Cubemap) != 0)
                {
                    layers *= 6;
                }
                TextureDescription desc = new TextureDescription(
                    texture.Width, texture.Height, texture.Depth,
                    texture.MipLevels, layers,
                    texture.Format,
                    TextureUsage.Staging, texture.Type);
                Texture readback = RF.CreateTexture(ref desc);
                CommandList cl = RF.CreateCommandList();
                cl.Begin();
                cl.CopyTexture(texture, readback);
                cl.End();
                GD.SubmitCommands(cl);
                GD.WaitForIdle();
                return readback;
            }
        }

        public void Dispose()
        {
            GD.WaitForIdle();
            _factory.DisposeCollector.DisposeAll();
            GD.Dispose();
            _window?.Close();
        }
    }

    public interface GraphicsDeviceCreator
    {
        void CreateGraphicsDevice(out Sdl2Window window, out GraphicsDevice gd);
    }

    public class VulkanDeviceCreator : GraphicsDeviceCreator
    {
        public void CreateGraphicsDevice(out Sdl2Window window, out GraphicsDevice gd)
        {
            window = null;
            gd = TestUtils.CreateVulkanDevice();
        }
    }

    public class VulkanDeviceCreatorWithMainSwapchain : GraphicsDeviceCreator
    {
        public unsafe void CreateGraphicsDevice(out Sdl2Window window, out GraphicsDevice gd)
        {
            TestUtils.CreateVulkanDeviceWithSwapchain(out window, out gd);
        }
    }
}
