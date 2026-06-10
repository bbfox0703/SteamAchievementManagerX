using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SAM.Game;
using Xunit;

public class KeyValueTests
{
    private static void WriteString(List<byte> buffer, string value)
    {
        buffer.AddRange(Encoding.UTF8.GetBytes(value));
        buffer.Add(0x00);
    }

    private static void WriteSection(List<byte> buffer, string name)
    {
        buffer.Add((byte)KeyValueType.None);
        WriteString(buffer, name);
    }

    private static void WriteInt32(List<byte> buffer, string name, int value)
    {
        buffer.Add((byte)KeyValueType.Int32);
        WriteString(buffer, name);
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    private static void WriteStringValue(List<byte> buffer, string name, string value)
    {
        buffer.Add((byte)KeyValueType.String);
        WriteString(buffer, name);
        WriteString(buffer, value);
    }

    private static void WriteEnd(List<byte> buffer)
    {
        buffer.Add((byte)KeyValueType.End);
    }

    // Mirrors the real UserGameStatsSchema_*.bin layout: gameid -> stats -> stat
    // -> values. Every section terminates at its own End byte (not EOF), which
    // is exactly the shape the recursion regression (commit 62feae2) failed to
    // parse because the nested call asserted Position == Length.
    private static byte[] BuildNestedSchema()
    {
        var buffer = new List<byte>();
        WriteSection(buffer, "583950");
        WriteSection(buffer, "stats");
        WriteSection(buffer, "1");
        WriteInt32(buffer, "type", 1);
        WriteStringValue(buffer, "name", "achievement_name");
        WriteEnd(buffer); // stat "1"
        WriteEnd(buffer); // stats
        WriteEnd(buffer); // gameid "583950"
        WriteEnd(buffer); // root
        return buffer.ToArray();
    }

    [Fact]
    public void ReadAsBinary_ParsesNestedSectionsAndResolvesValues()
    {
        var kv = new KeyValue();
        using var stream = new MemoryStream(BuildNestedSchema());

        Assert.True(kv.ReadAsBinary(stream));

        var stat = kv["583950"]["stats"]["1"];
        Assert.Equal(1, stat["type"].AsInteger(-1));
        Assert.Equal("achievement_name", stat["name"].AsString(string.Empty));
    }

    [Fact]
    public void ReadAsBinary_ReturnsFalseWhenStreamHasTrailingData()
    {
        var bytes = new List<byte>(BuildNestedSchema()) { 0xFF };
        var kv = new KeyValue();
        using var stream = new MemoryStream(bytes.ToArray());

        Assert.False(kv.ReadAsBinary(stream));
    }
}
