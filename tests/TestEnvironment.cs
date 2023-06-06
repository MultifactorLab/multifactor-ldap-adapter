using System.IO;
using System;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures
{
    internal enum TestAssetLocation
    {
        RootDirectory,
        ClientsDirectory
    }

    internal static class TestEnvironment
    {
        private static readonly string _appFolder = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}";
        private static readonly string _assetsFolder = $"{_appFolder}{Path.DirectorySeparatorChar}Assets";

        public static string GetAssetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return _assetsFolder;
            return $"{_assetsFolder}{Path.DirectorySeparatorChar}{fileName}";
        }

        public static string GetAssetPath(TestAssetLocation location)
        {
            switch (location)
            {
                case TestAssetLocation.ClientsDirectory: return $"{_assetsFolder}{Path.DirectorySeparatorChar}clients";
                case TestAssetLocation.RootDirectory:
                default: return _assetsFolder;
            }
        }

        public static string GetAssetPath(TestAssetLocation location, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return GetAssetPath(location);
            return $"{GetAssetPath(location)}{Path.DirectorySeparatorChar}{fileName}";
        }
    }
}