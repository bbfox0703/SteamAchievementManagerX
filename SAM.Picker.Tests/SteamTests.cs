using System;
using System.Runtime.InteropServices;
using System.Text;
using SAM.API;
using Xunit;

namespace SAM.Picker.Tests
{
    public class SteamTests
    {
        private static class Native
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern int GetDllDirectory(int nBufferLength, StringBuilder lpBuffer);
        }

        [Fact]
        public void LoadDoesNotChangeDllDirectory()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            const int capacity = 260;
            var before = new StringBuilder(capacity);
            Native.GetDllDirectory(capacity, before);

            Steam.Load();
            Steam.Unload();

            var after = new StringBuilder(capacity);
            Native.GetDllDirectory(capacity, after);

            Assert.Equal(before.ToString(), after.ToString());
        }
    }
}
