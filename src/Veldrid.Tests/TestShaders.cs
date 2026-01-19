using System;
using System.IO;
using Veldrid.LowLevelRenderer.Core;
using Veldrid.SPIRV;

namespace Veldrid.Tests
{
    internal static class TestShaders
    {
        public static Shader[] LoadVertexFragment(ResourceFactory factory, string setName)
        {
            return new[]
            {
                factory.CreateShader(
                    new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes(GetPath(setName, ShaderStages.Vertex)), "main")),
                factory.CreateShader(
                    new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes(GetPath(setName, ShaderStages.Fragment)), "main"))
            };
        }

        public static Shader LoadCompute(ResourceFactory factory, string setName)
        {
            return factory.CreateShader(
                new ShaderDescription(ShaderStages.Compute, File.ReadAllBytes(GetPath(setName, ShaderStages.Compute)), "main"));
        }

        public static string GetPath(string setName, ShaderStages stage)
        {
            return Path.Combine(
                AppContext.BaseDirectory,
                "Shaders",
                $"{setName}.{stage.ToString().ToLowerInvariant().Substring(0, 4)}");
        }
    }
}
