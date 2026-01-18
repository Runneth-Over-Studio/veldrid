using System;
using System.Collections.Generic;
using System.IO;

namespace Veldrid.NeoDemo
{
    public static class ShaderHelper
    {
        public static (Shader vs, Shader fs) LoadSPIRV(
            GraphicsDevice gd,
            ResourceFactory factory,
            string setName)
        {
            byte[] vsBytes = LoadBytecode(GraphicsBackend.Vulkan, setName, ShaderStages.Vertex);
            byte[] fsBytes = LoadBytecode(GraphicsBackend.Vulkan, setName, ShaderStages.Fragment);
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            Shader vs = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vsBytes, "main", debug));
            Shader fs = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fsBytes, "main", debug));

            vs.Name = setName + "-Vertex";
            fs.Name = setName + "-Fragment";

            return (vs, fs);
        }

        public static SpecializationConstant[] GetSpecializations(GraphicsDevice gd)
        {
            bool glOrGles = false;

            List<SpecializationConstant> specializations =
            [
                new SpecializationConstant(100, gd.IsClipSpaceYInverted),
                new SpecializationConstant(101, glOrGles), // TextureCoordinatesInvertedY
                new SpecializationConstant(102, gd.IsDepthRangeZeroToOne),
            ];

            PixelFormat swapchainFormat = gd.MainSwapchain.Framebuffer.OutputDescription.ColorAttachments[0].Format;
            bool swapchainIsSrgb = swapchainFormat == PixelFormat.B8_G8_R8_A8_UNorm_SRgb
                || swapchainFormat == PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
            specializations.Add(new SpecializationConstant(103, swapchainIsSrgb));

            return specializations.ToArray();
        }

        public static byte[] LoadBytecode(GraphicsBackend backend, string setName, ShaderStages stage)
        {
            string stageExt = stage == ShaderStages.Vertex ? "vert" : "frag";
            string name = setName + "." + stageExt;

            if (backend == GraphicsBackend.Vulkan)
            {
                string bytecodeExtension = GetBytecodeExtension(backend);
                string bytecodePath = AssetHelper.GetPath(Path.Combine("Shaders", name + bytecodeExtension));

                if (File.Exists(bytecodePath))
                {
                    byte[] bytes = File.ReadAllBytes(bytecodePath);

                    if (bytes.Length >= 4)
                    {
                        uint magicNumber = BitConverter.ToUInt32(bytes, 0);
                        if (magicNumber != 0x07230203)
                        {
                            throw new InvalidOperationException($"File {bytecodePath} is not valid SPIR-V (magic number: 0x{magicNumber:X8})");
                        }
                    }
                    return bytes;
                }
                else
                {
                    throw new FileNotFoundException(
                        $"Expected SPIR-V file not found: {bytecodePath}. " +
                        $"Ensure shaders are compiled to .spv format for Vulkan.");
                }
            }

            string extension = GetSourceExtension(backend);
            string path = AssetHelper.GetPath(Path.Combine("Shaders.Generated", name + extension));
            return File.ReadAllBytes(path);
        }

        private static string GetBytecodeExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Vulkan: return ".spv";
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }

        private static string GetSourceExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Vulkan: return ".450.glsl";
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }
    }
}
