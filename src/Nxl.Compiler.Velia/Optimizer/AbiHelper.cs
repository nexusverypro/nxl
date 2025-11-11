using System;
using Re.Asm;

namespace Nxl.Compiler.Velia.Optimizer;

public enum Abi
{
    SystemV,
    WindowsX64
}

public static class AbiHelper
{
    private static readonly RegisterOperand[] LinuxIntArgRegs = { Register.RDI, Register.RSI, Register.RDX, Register.RCX, Register.R8, Register.R9 };
    private static readonly RegisterOperand[] WindowsIntArgRegs = { Register.RCX, Register.RDX, Register.R8, Register.R9 };

    public static Generator LoadFunctionCallArgs(this Generator generator, Abi abi, ReadOnlySpan<IOperand> args)
    {
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        RegisterOperand[] intArgRegs = abi switch
        {
            Abi.SystemV => LinuxIntArgRegs,
            Abi.WindowsX64 => WindowsIntArgRegs,
            _ => throw new ArgumentOutOfRangeException(nameof(abi))
        };

        int regCount = intArgRegs.Length;
        int argCount = args.Length;

        // load arguments into registers
        int i = 0;
        for (; i < Math.Min(argCount, regCount); i++)
        {
            if (args[i] is RegisterOperand regOp)
                generator.Mov(intArgRegs[i], regOp);
            else if (args[i] is Imm8Operand imm8)
                generator.Mov(intArgRegs[i], imm8);
            else if (args[i] is Imm16Operand imm16)
                generator.Mov(intArgRegs[i], imm16);
            else if (args[i] is Imm32Operand imm32)
                generator.Mov(intArgRegs[i], imm32);
            else if (args[i] is Imm64Operand imm64)
                generator.Mov(intArgRegs[i], imm64);
        }

        // push extra arguments
        if (i < argCount)
        {
            for (int j = argCount - 1; j >= regCount; j--)
            {
                if (args[j] is RegisterOperand regOp)
                    generator.Push(regOp);
                else if (args[j] is Imm8Operand imm8)
                    generator.Push(imm8);
                else if (args[j] is Imm32Operand imm32)
                    generator.Push(imm32);
            }
        }

        // windows shadow space
        if (abi == Abi.WindowsX64)
            generator.Sub(Register.RSP, Immediate.Byte(32)); // reserve 32 bytes shadow space

        // ensure 16-byte alignment
        int extraArgs = Math.Max(argCount - regCount, 0);
        int stackBytes = extraArgs * 8;
        if (abi == Abi.WindowsX64) stackBytes += 32; // shadow space

        byte padding = (byte)((16 - (stackBytes % 16)) % 16);
        if (padding > 0) generator.Sub(Register.RSP, Immediate.Byte(padding));

        return generator;
    }

    public static Generator LoadFunctionCallLocals(
        this Generator generator,
        Abi abi,
        int argCount,
        RegisterOperand[] locals)
    {
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (locals.Length < argCount) throw new ArgumentException("Not enough local registers supplied");

        RegisterOperand[] argRegs = abi switch
        {
            Abi.SystemV => LinuxIntArgRegs,
            Abi.WindowsX64 => WindowsIntArgRegs,
            _ => throw new ArgumentOutOfRangeException(nameof(abi))
        };

        int regCount = argRegs.Length;

        // copy register arguments into local variables
        int i = 0;
        for (; i < Math.Min(argCount, regCount); i++)
            generator.Mov(locals[i], argRegs[i]);

        // copy stack arguments into locals
        if (i < argCount)
        {
            // rsp + offset, offset can change based on abi
            int stackOffset = 0;
            if (abi == Abi.WindowsX64)
                stackOffset += 32; // skip shadow space

            for (int j = i; j < argCount; j++)
            {
                generator.Mov(locals[j], Memory.Base(Register.RSP, stackOffset));
                stackOffset += 8; // each stack arg is 8 bytes
            }
        }

        return generator;
    }
}
