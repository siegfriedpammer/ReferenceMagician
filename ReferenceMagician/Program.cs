// Copyright (c) 2017 Siegfried Pammer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NDesk.Options;
using Mono.Cecil;

namespace ReferenceMagician
{
	class Program
	{
		static readonly string[] BCL_Hashes = {
			"b77a5c561934e089"
		};

		static void Main(string[] args)
		{
			bool printHelp = false;
			bool includeGacAssemblies = true;
			bool includeBclAssemblies = false;
			string outputDirectory = null;

			var options = new OptionSet {
				{ "h|help", "show this message and exit", v => printHelp = v != null },
				{ "o|out|output=", "output directory for all assemblies, uses 'lib' if not specified", v => outputDirectory = v },
				{ "nogac", "if specified, GAC (Global Assembly Cache) assemblies are ignored", v => includeGacAssemblies = v == null },
				{ "includebcl|bcl", "if specified, BCL assemblies are included", v => includeBclAssemblies = v != null }
			};
			List<string> assemblies;
			try {
				assemblies = options.Parse(args);
			} catch (OptionException ex) {
				Console.WriteLine(ex.Message);
				PrintHelp(options);
				return;
			}

			if (!printHelp) {
				if (!assemblies.Any()) {
					Console.WriteLine("No assemblies specified!");
					PrintHelp(options);
					return;
				}

				if (string.IsNullOrWhiteSpace(outputDirectory))
					outputDirectory = "lib\\";
				Console.WriteLine("Assemblies: " + string.Join(", ", assemblies));
				Console.WriteLine("Output: " + outputDirectory);
				Directory.CreateDirectory(outputDirectory);
				foreach (var assembly in assemblies) {
					CopyAllReferences(Path.GetDirectoryName(assembly), assembly, outputDirectory, includeGacAssemblies, includeBclAssemblies);
				}
				return;
			}

			PrintHelp(options);
		}

		static void CopyAllReferences(string baseDirectory, string fileName, string outputDirectory, bool includeGacAssemblies, bool includeBclAssemblies)
		{
			Queue<string> queue = new Queue<string>();
			HashSet<string> copiedFiles = new HashSet<string>();
			queue.Enqueue(fileName);

			while (queue.Count > 0) {
				var currentFile = queue.Dequeue();
				var assembly = Load(currentFile);
				if (assembly == null) continue;
				var outputFileName = Path.Combine(outputDirectory, Path.GetFileName(currentFile));
				Console.WriteLine($"Copying {currentFile} to {outputFileName}...");
				try {
					File.Copy(currentFile, outputFileName);
				} catch (IOException ex) when ((uint)ex.HResult == 0x80070050) {
					Console.WriteLine($"WARNING: Skipping {Path.GetFileName(currentFile)}: File already exists!");
				}
				foreach (var reference in assembly.MainModule.AssemblyReferences) {
					var file = ResolveReference(baseDirectory, reference, !includeGacAssemblies);
					if (includeGacAssemblies && !includeBclAssemblies) {
						if (BCL_Hashes.Any(hash => string.Equals(hash, HashToString(reference.PublicKeyToken), StringComparison.OrdinalIgnoreCase)))
							continue;
					}
					if (string.IsNullOrWhiteSpace(file))
						continue;
					if (copiedFiles.Add(file))
						queue.Enqueue(file);
				}
			}
		}

		static string HashToString(byte[] publicKeyToken)
		{
			var builder = new StringBuilder(publicKeyToken.Length * 2);

			for (int i = 0; i < publicKeyToken.Length; i++) {
				builder.AppendFormat("{0:x}", publicKeyToken[i]);
			}

			return builder.ToString();
		}

		static string ResolveReference(string baseDirectory, AssemblyNameReference reference, bool ignoreGacAssembly)
		{
			string file = null;
			if (!ignoreGacAssembly)
				file = GacInterop.FindAssemblyInNetGac(reference);
			if (file == null) {
				if (File.Exists(Path.Combine(baseDirectory, reference.Name + ".dll")))
					file = Path.Combine(baseDirectory, reference.Name + ".dll");
				else if (File.Exists(Path.Combine(baseDirectory, reference.Name + ".exe")))
					file = Path.Combine(baseDirectory, reference.Name + ".exe");
			}
			return file;
		}

		static AssemblyDefinition Load(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return null;
			try {
				return AssemblyDefinition.ReadAssembly(fileName);
			} catch (Exception ex) {
				Console.WriteLine($"{fileName}: " + ex.Message);
				return null;
			}
		}

		static void PrintHelp(OptionSet options)
		{
			Console.WriteLine("Usage: referencemagician [OPTIONS]+ Assembly+");
			Console.WriteLine("Collects all transitive references of each specified assembly.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			options.WriteOptionDescriptions(Console.Out);
		}
	}
}
