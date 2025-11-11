using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nxl.Compiler.Velia.Nodes
{
    public static class VeliaAst
    {
        private static List<bool> _lastAtLevel = new List<bool>();

        public static void PrintDebugString(VeliaSyntaxNode? syntaxNode, int indent = 0, bool isLast = true)
        {
            try
            {
                if (syntaxNode == null) return;
                if (_lastAtLevel.Count <= indent)
                {
                    _lastAtLevel.AddRange(Enumerable.Repeat(false, indent + 1 - _lastAtLevel.Count));
                }

                if (indent > 0) _lastAtLevel[indent - 1] = isLast;
                for (int i = 0; i < indent; i++)
                {
                    if (i == indent - 1)
                        Console.Write(isLast ? "`-" : "|-");
                    else Console.Write(_lastAtLevel[i] ? "  " : "| ");
                }

                Console.WriteLine(syntaxNode.ToFullString());
                for (int i = 0; i < syntaxNode.SlotCount; i++)
                {
                    var childNode = syntaxNode.GetSlot(i);
                    PrintDebugString(childNode as VeliaSyntaxNode, indent + 1, i == syntaxNode.SlotCount - 1);
                }
            }
            finally { _lastAtLevel.Clear(); }
        }
    }
}
