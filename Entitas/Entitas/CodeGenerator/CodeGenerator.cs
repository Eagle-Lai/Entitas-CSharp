using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Entitas.CodeGenerator {
    public static class CodeGenerator {
        public const string DEFAULT_POOL_NAME = "Pool";
        public const string COMPONENT_SUFFIX = "Component";
        public const string DEFAULT_COMPONENT_LOOKUP_TAG = "ComponentIds";
        public const string AUTO_GENERATED_HEADER_FORMAT = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by {0}.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
";

        public static CodeGenFile[] Generate(ICodeGeneratorDataProvider provider, string directory, ICodeGenerator[] codeGenerators) {
            directory = GetSafeDir(directory);
            CleanDir(directory);

            var generatedFiles = new List<CodeGenFile>();
            var componentInfos = provider.componentInfos;

            foreach (var generator in codeGenerators.OfType<IPoolCodeGenerator>()) {
                var files = generator.Generate(provider.poolNames, componentInfos);
                generatedFiles.AddRange(files);
                writeFiles(directory, files);
            }

            foreach (var generator in codeGenerators.OfType<IComponentCodeGenerator>()) {
                var files = generator.Generate(componentInfos);
                generatedFiles.AddRange(files);
                writeFiles(directory, files);
            }

            foreach (var generator in codeGenerators.OfType<IBlueprintsCodeGenerator>()) {
                var files = generator.Generate(provider.blueprintNames);
                generatedFiles.AddRange(files);
                writeFiles(directory, files);
            }

            return generatedFiles.ToArray();
        }

        public static string GetSafeDir(string directory) {
            if (!directory.EndsWith("/", StringComparison.Ordinal)) {
                directory += "/";
            }
            if (!directory.EndsWith("Generated/", StringComparison.Ordinal)) {
                directory += "Generated/";
            }
            return directory;
        }

        public static void CleanDir(string directory) {
            directory = GetSafeDir(directory);
            if (Directory.Exists(directory)) {
                var files = new DirectoryInfo(directory).GetFiles("*.cs", SearchOption.AllDirectories);
                foreach (var file in files) {
                    try {
                        File.Delete(file.FullName);
                    } catch {
                        Console.WriteLine("Could not delete file " + file);
                    }
                }
            } else {
                Directory.CreateDirectory(directory);
            }
        }

        static void writeFiles(string directory, CodeGenFile[] files) {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            foreach (var file in files) {
                var fileName = directory + file.fileName + ".cs";
                var fileContent = file.fileContent.Replace("\n", Environment.NewLine);
                var header = string.Format(AUTO_GENERATED_HEADER_FORMAT, file.generatorName);
                File.WriteAllText(fileName, header + fileContent);
            }
        }
    }

    public static class CodeGeneratorExtensions {

        public static string[] ComponentLookupTags(this ComponentInfo componentInfo) {
            return componentInfo.pools
                .Select(poolName => poolName.PoolPrefix() + CodeGenerator.DEFAULT_COMPONENT_LOOKUP_TAG)
                .ToArray();
        }

        public static string PoolPrefix(this string poolName) {
            return poolName == CodeGenerator.DEFAULT_POOL_NAME ? string.Empty : poolName;
        }

        public static string UppercaseFirst(this string str) {
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static string LowercaseFirst(this string str) {
            return char.ToLower(str[0]) + str.Substring(1);
        }

        public static string ToUnixLineEndings(this string str) {
            return str.Replace(Environment.NewLine, "\n");
        }
    }
}
