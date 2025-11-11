#define TEST_COMPILATION_EXAMPLE_HELLO_WORLD

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nxl.Compiler.Velia
{
    public static class Program
    {
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
            if (argument == "--project") return "E:\\Development\\.github\\@nexusverypro\\nxl-cs\\examples\\hello-world";
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