using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

namespace Nxl.Compiler.Velia
{
    public struct ProjectStructure
    {
        public string FileName;
        public string Name;
        public string Description;
        public string Version;
        public string Author;
        public string License;
        public string DirectoryPath;
        public string IntermediatePath;
        public string OutputPath;
        public string Configuration;
        public int OptimizationLevel;
        public bool UseUnsafeCode;
        public string[] Exclude;
        public string[] Include;

        public static ProjectStructure? Parse(string directory, string configuration = "debug")
        {
            string projectFilePath = Path.Combine(directory, "project.toml");
            if (!File.Exists(projectFilePath)) return null;

            string tomlText = File.ReadAllText(projectFilePath);
            var model = Toml.ToModel(tomlText);
            var project = new ProjectStructure
            {
                DirectoryPath = directory // keep original project directory
            };

            // project section
            if (model.TryGetValue("project", out var projectTable) && projectTable is TomlTable pt)
            {
                project.FileName = pt.TryGetValue("file_name", out var f) ? f?.ToString() ?? "" : "";
                project.Name = pt.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";
                project.Description = pt.TryGetValue("description", out var d) ? d?.ToString() ?? "" : "";
                project.Version = pt.TryGetValue("version", out var v) ? v?.ToString() ?? "" : "";
                project.Author = pt.TryGetValue("author", out var a) ? a?.ToString() ?? "" : "";
                project.License = pt.TryGetValue("license", out var l) ? l?.ToString() ?? "" : "";
            }

            // configuration section
            if (model.TryGetValue("configuration", out var configTable) && configTable is TomlTable ct)
            {
                if (ct.TryGetValue(configuration, out var conf) && conf is TomlTable cfg)
                {
                    project.OutputPath = cfg.TryGetValue("output", out var o) ? Path.Combine(directory, o?.ToString() ?? "") : Path.Combine(directory, "bin", configuration);
                    project.IntermediatePath = Path.Combine(project.OutputPath, ".int");

                    project.OptimizationLevel = cfg.TryGetValue("optimization", out var opt) && int.TryParse(opt?.ToString(), out var level) ? level : 0;
                    project.UseUnsafeCode = cfg.TryGetValue("unsafe", out var u) && bool.TryParse(u?.ToString(), out var useUnsafe) ? useUnsafe : false;
                }
            }

            // structure section
            if (model.TryGetValue("structure", out var structureTable) && structureTable is TomlTable st)
            {
                project.Exclude = st.TryGetValue("exclude", out var excl) && excl is TomlArray exclArr ? ArrayFromTomlArray(exclArr) : Array.Empty<string>();
                project.Include = st.TryGetValue("include", out var incl) && incl is TomlArray inclArr ? ArrayFromTomlArray(inclArr) : Array.Empty<string>();
            }

            return project;
        }

        private static string[] ArrayFromTomlArray(TomlArray arr)
        {
            var result = new string[arr.Count];
            for (int i = 0; i < arr.Count; i++)
                result[i] = arr[i]?.ToString() ?? "";
            return result;
        }
    }
}
