/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

 #if WINDOWS
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;

namespace SAM.API
{
    public static class Steam
    {
        private struct Native
        {
            [DllImport("kernel32.dll", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr GetProcAddress(IntPtr module, string name);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeLibrary(IntPtr module);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr AddDllDirectory(string path);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool RemoveDllDirectory(IntPtr handle);

            internal const uint LoadLibrarySearchDefaultDirs = 0x00001000;
            internal const uint LoadLibrarySearchUserDirs = 0x00000400;
        }

        private static Delegate GetExportDelegate<TDelegate>(IntPtr module, string name)
        {
            IntPtr address = Native.GetProcAddress(module, name);
            return address == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer(address, typeof(TDelegate));
        }

        private static TDelegate GetExportFunction<TDelegate>(IntPtr module, string name)
            where TDelegate : class
        {
            return (TDelegate)((object)GetExportDelegate<TDelegate>(module, name));
        }

        private static IntPtr _Handle = IntPtr.Zero;

        public static string GetInstallPath()
        {
            const string subKey = @"Software\Valve\Steam";

            // Query 64-bit view first, then fall back to 32-bit view (WOW6432Node)
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(subKey))
                {
                    if (key == null)
                    {
                        continue;
                    }

                    var path = key.GetValue("InstallPath") as string;
                    if (string.IsNullOrEmpty(path) == false)
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate IntPtr NativeCreateInterface(string version, IntPtr returnCode);

        private static NativeCreateInterface _CallCreateInterface;

        public static TClass CreateInterface<TClass>(string version)
            where TClass : INativeWrapper, new()
        {
            IntPtr address = _CallCreateInterface(version, IntPtr.Zero);

            if (address == IntPtr.Zero)
            {
                return default;
            }

            TClass instance = new();
            instance.SetupFunctions(address);
            return instance;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool NativeSteamGetCallback(int pipe, out Types.CallbackMessage message, out int call);

        private static NativeSteamGetCallback _CallSteamBGetCallback;

        public static bool GetCallback(int pipe, out Types.CallbackMessage message, out int call)
        {
            return _CallSteamBGetCallback(pipe, out message, out call);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool NativeSteamFreeLastCallback(int pipe);

        private static NativeSteamFreeLastCallback _CallSteamFreeLastCallback;

        public static bool FreeLastCallback(int pipe)
        {
            return _CallSteamFreeLastCallback(pipe);
        }

        public static bool Load()
        {
            if (_Handle != IntPtr.Zero)
            {
                return true;
            }

            string path = GetInstallPath();
            if (path == null)
            {
                return false;
            }

            var binPath = Path.Combine(path, "bin");
            string library = Environment.Is64BitProcess ? "steamclient64.dll" : "steamclient.dll";
            string libraryPath = Path.Combine(path, library);
            if (File.Exists(libraryPath) == false)
            {
                libraryPath = Path.Combine(binPath, library);
                if (File.Exists(libraryPath) == false)
                {
                    return false;
                }
            }

            IntPtr pathHandle = IntPtr.Zero;
            IntPtr binHandle = IntPtr.Zero;
            try
            {
                pathHandle = Native.AddDllDirectory(path);
                if (pathHandle == IntPtr.Zero)
                {
                    return false;
                }

                binHandle = Native.AddDllDirectory(binPath);
                if (binHandle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(libraryPath));
                    if (certificate.Verify() == false)
                    {
                        return false;
                    }

                    // Pin the certificate identity to Valve's known subject
                    const string ValveSubject = "CN=Valve Corp., O=Valve Corp., L=Bellevue, S=Washington, C=US";
                    var subject = certificate.Subject;
                    if (string.Equals(subject, ValveSubject, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }

                IntPtr module = Native.LoadLibraryEx(
                    libraryPath,
                    IntPtr.Zero,
                    Native.LoadLibrarySearchDefaultDirs | Native.LoadLibrarySearchUserDirs);
                if (module == IntPtr.Zero)
                {
                    return false;
                }

                _CallCreateInterface = GetExportFunction<NativeCreateInterface>(module, "CreateInterface");
                if (_CallCreateInterface == null)
                {
                    return false;
                }

                _CallSteamBGetCallback = GetExportFunction<NativeSteamGetCallback>(module, "Steam_BGetCallback");
                if (_CallSteamBGetCallback == null)
                {
                    return false;
                }

                _CallSteamFreeLastCallback = GetExportFunction<NativeSteamFreeLastCallback>(module, "Steam_FreeLastCallback");
                if (_CallSteamFreeLastCallback == null)
                {
                    return false;
                }

                _Handle = module;
                return true;
            }
            finally
            {
                if (binHandle != IntPtr.Zero)
                {
                    Native.RemoveDllDirectory(binHandle);
                }

                if (pathHandle != IntPtr.Zero)
                {
                    Native.RemoveDllDirectory(pathHandle);
                }
            }
        }
        public static void Unload()
        {
            if (_Handle != IntPtr.Zero)
            {
                Native.FreeLibrary(_Handle);
                _Handle = IntPtr.Zero;
            }
        }
    }
}
#else
using System;
using Steamworks;

namespace SAM.API
{
    public static class Steam
    {
        public static string GetInstallPath() =>
            Environment.GetEnvironmentVariable("STEAM_PATH");

        public static bool Load() => SteamAPI.Init();

        public static void Unload() => SteamAPI.Shutdown();

        public static TClass CreateInterface<TClass>(string version)
            where TClass : INativeWrapper, new() => default;

        public static bool GetCallback(int pipe, out Types.CallbackMessage message, out int call)
        {
            SteamAPI.RunCallbacks();
            message = default;
            call = 0;
            return false;
        }

        public static bool FreeLastCallback(int pipe) => true;
    }
}
#endif
