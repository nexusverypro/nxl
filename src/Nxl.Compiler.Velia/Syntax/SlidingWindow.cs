using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nxl.Compiler.Velia.Syntax;

public sealed class SlidingWindow<TElement> : IEnumerable<TElement>
{
    private readonly IList<TElement> _buffer;
    private int _position;

    public SlidingWindow(IEnumerable<TElement> elements)
    {
        if (elements is IList<TElement> list)
            _buffer = list;
        else _buffer = elements.ToList();

        _position = 0;
    }

    public TElement Forward()
    {
        if (_position + 1 >= _buffer.Count)
            throw new InvalidOperationException("Cannot move forward beyond the end of the window.");

        _position++;
        return _buffer[_position];
    }

    public TElement Forward(int offset)
    {
        if (_position + offset >= _buffer.Count)
            throw new InvalidOperationException("Cannot move forward beyond the end of the window.");

        _position += offset;
        return _buffer[_position];
    }

    public TElement Peek()
    {
        if (_position >= _buffer.Count)
            throw new InvalidOperationException("Peek beyond end of buffer.");
        return _buffer[_position];
    }

    public TElement Peek(int offset)
    {
        int target = _position + offset;
        if (target < 0 || target >= _buffer.Count)
            throw new InvalidOperationException("Peek offset is out of range.");
        return _buffer[target];
    }

    public TElement Back()
    {
        if (_position - 1 < 0)
            throw new InvalidOperationException("Cannot move backward beyond start of the window.");

        _position--;
        return _buffer[_position];
    }

    public TElement Back(int offset)
    {
        if (_position - offset < 0)
            throw new InvalidOperationException("Cannot move backward beyond start of the window.");

        _position -= offset;
        return _buffer[_position];
    }

    public bool IsEndOfWindow(int offset) => (_position + offset) >= _buffer.Count;

    public IEnumerator<TElement> GetEnumerator() => _buffer.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Position => _position;
    public int Count => _buffer.Count;
    public int Left => Math.Max(0, _buffer.Count - (_position + 1));
    public bool EndOfWindow => _position >= _buffer.Count - 1;
    public TElement PreviousElement => Peek(-1);
    public TElement CurrentElement => Peek(0);
    public TElement NextElement => Peek(1);

    public override string ToString()
        => $"SlidingWindow(Position={_position}, Count={_buffer.Count}, Left={Left})";
}
