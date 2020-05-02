using System;

// ReSharper disable once CheckNamespace
namespace WebDav
{
    internal static class RuntimeUtils
    {
        public static bool IsBlazorWASM => Type.GetType("Mono.Runtime") != null;
    }
}