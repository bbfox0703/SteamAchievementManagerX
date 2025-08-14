using System;
using Xunit;
using SAM.API;

namespace SAM.Picker.Tests
{
    public class NativeStringsTests
    {
        [Fact]
        public unsafe void PointerToString_DoesNotReadPastProvidedLength()
        {
            var buffer = stackalloc sbyte[6];
            buffer[0] = (sbyte)'a';
            buffer[1] = (sbyte)'b';
            buffer[2] = (sbyte)'c';
            buffer[3] = (sbyte)'d';
            buffer[4] = (sbyte)'e';
            buffer[5] = 0;

            var result = NativeStrings.PointerToString(buffer, 3);
            Assert.Equal("abc", result);
        }

        [Fact]
        public unsafe void PointerToString_NoTerminatorWithinLength()
        {
            var buffer = stackalloc sbyte[5];
            buffer[0] = (sbyte)'a';
            buffer[1] = (sbyte)'b';
            buffer[2] = (sbyte)'c';
            buffer[3] = (sbyte)'d';
            buffer[4] = (sbyte)'e';

            var result = NativeStrings.PointerToString(buffer, 5);
            Assert.Equal("abcde", result);
        }

        [Fact]
        public unsafe void PointerToString_NullPointerReturnsNull()
        {
            sbyte* buffer = null;
            var result = NativeStrings.PointerToString(buffer, 5);
            Assert.Null(result);
        }
    }
}
