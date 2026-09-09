using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
class Format {
 static void Main(string[] args) {
  foreach(var file in Directory.GetFiles(args[0],"*.cs",SearchOption.AllDirectories)) {
   var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file));
   File.WriteAllText(file,tree.GetRoot().NormalizeWhitespace("    ","\n").ToFullString()+"\n");
  }
 }
}
