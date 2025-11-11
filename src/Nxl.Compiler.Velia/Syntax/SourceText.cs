using System;
using System.Collections.Immutable;
using System.Text;

namespace Nxl.Compiler.Velia.Syntax;

public abstract class SourceText : IEquatable<SourceText>
{
    public abstract Encoding CurrentEncoding { get; }
    public abstract ImmutableArray<byte> Text { get; }
    public int Length => Text.Length;

    public string GetString(int index = 0, int length = -1)
    {
        if (index < 0 || index > Text.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        if (length < 0) length = Text.Length - index;
        if (index + length > Text.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length extends beyond the end of the text.");

        var slice = Text.Slice(index, length).ToArray();
        return CurrentEncoding.GetString(slice);
    }

    public bool Equals(SourceText? other)
    {
        if (other == null) return false;
        return Text.SequenceEqual(other.Text) && CurrentEncoding.Equals(other.CurrentEncoding);
    }

    public override bool Equals(object? obj) => obj is SourceText other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(CurrentEncoding, Text.Length);
}

public sealed class StringText : SourceText
{
    private readonly Encoding _currentEncoding;
    private readonly ImmutableArray<byte> _text;

    private StringText(Encoding currentEncoding, ImmutableArray<byte> text)
    {
        _currentEncoding = currentEncoding;
        _text = text;
    }

    public static SourceText Create(Encoding currentEncoding, string filePath)
    {
        const int CHUNK_SIZE = 64; // 64 bytes per chunk

        using var fileStream = File.OpenRead(filePath);
        var textBuilder = ImmutableArray.CreateBuilder<byte>((int)fileStream.Length);

        byte[] buffer = new byte[CHUNK_SIZE];
        int bytesRead;
        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            for (int i = 0; i < bytesRead; i++)
                textBuilder.Add(buffer[i]);

        return new StringText(currentEncoding, textBuilder.ToImmutable());
    }

    public override Encoding CurrentEncoding => _currentEncoding;
    public override ImmutableArray<byte> Text => _text;
}