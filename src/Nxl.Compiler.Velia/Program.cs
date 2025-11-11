#define TEST_COMPILATION_EXAMPLE_HELLO_WORLD

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Re.Asm;

namespace Nxl.Compiler.Velia
{
    public static class ReadonlyStaticPatcher
    {
        public static IntPtr GetStaticFieldAddress(FieldInfo field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (!field.IsStatic) throw new ArgumentException("Field must be static.", nameof(field));

            var dm = new DynamicMethod(
                $"__getaddr_{Guid.NewGuid():N}",
                typeof(IntPtr),
                Type.EmptyTypes,
                field.DeclaringType ?? throw new InvalidOperationException(),
                skipVisibility: true);

            var il = dm.GetILGenerator();
            {
                il.Emit(OpCodes.Ldsflda, field);    // push address of static field
                il.Emit(OpCodes.Conv_I);            // convert to native int
                il.Emit(OpCodes.Ret);               // return ptr
            }

            return ((Func<IntPtr>)dm.CreateDelegate(typeof(Func<IntPtr>))).Invoke();
        }

        public static unsafe void WriteValueToAddress<T>(IntPtr address, T value) where T : struct
        {
            if (address == IntPtr.Zero) throw new ArgumentNullException(nameof(address));
            Unsafe.Write(address.ToPointer(), value);
        }
    }

    public static class Program
    {
        private static void HackFixInvalidRegisterNames()
        {
            var field = typeof(Re.Asm.Register).GetField("Invalid", BindingFlags.Public | BindingFlags.Static);
            if (field == null) throw new Exception("Field not found.");

            var newValue = new Re.Asm.Register.Info(string.Empty, 0);
            ReadonlyStaticPatcher.WriteValueToAddress(
                ReadonlyStaticPatcher.GetStaticFieldAddress(field),
                newValue
            );
        }

        private static bool HasArgument(string argument)
        {
#if TEST_COMPILATION_EXAMPLE_HELLO_WORLD
            if (argument == "--save-temp") return true;
            if (argument == "--config") return true;
            if (argument == "--project") return true;
#endif
            var args = Environment.GetCommandLineArgs();
            return args.Contains(argument);
        }

        private static string? GetArgument(string argument)
        {
#if TEST_COMPILATION_EXAMPLE_HELLO_WORLD
            if (argument == "--config") return "debug";
            if (argument == "--project") return "/run/media/nex/32 GB/GitHub/@nexusverypro/nxl/examples/hello-world";
#endif
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, argument);
            if (index == -1 || index + 1 >= args.Length) return null;
            return args[index + 1];
        }

        public static async Task<int> Main(string[] args)
        {
            if (!HasArgument("--project"))
            {
                await Console.Error.WriteLineAsync("--project argument needed");
                return 1;
            }

            // i hate you, me
            HackFixInvalidRegisterNames();

            // load project structure
            var projectStructure = ProjectStructure.Parse(
                GetArgument("--project") ?? throw new InvalidOperationException("Unreachable code"),
                GetArgument("--config") ?? "debug");
            if (projectStructure == null || !projectStructure.HasValue)
            {
                await Console.Error.WriteLineAsync("invalid project path or configuration");
                return 1;
            }

            // start compilation
            var client = new CompilationClient(
                projectStructure.Value,
                saveTemp: HasArgument("--save-temp")
            );
            var result = await client.CompileAsync();

            // handle result
            if (result.IsSuccess)
            {
                await Console.Out.WriteLineAsync(
                    $"written to {result.OutputPath} in {result.ElapsedMilliseconds}ms");
                return 0;
            }
            else
            {
                await Console.Error.WriteLineAsync($"-> aborting: {result.ErrorMessage}");
                return 1;
            }
        }
    }
}