using System;

namespace Nxl.Compiler.Velia;

public sealed class StdOutErrException : Exception
{
    public StdOutErrException(string message, string stdout, string strerr)
        : base(string.Format("{0}.\nstdout: {1}\nstderr: {2}", message, stdout, strerr))
    {
        
    }
}
