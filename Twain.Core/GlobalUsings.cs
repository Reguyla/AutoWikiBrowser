global using System;
// Temporary compatibility while migrating CustomModule compilation
// from CodeDOM to Roslyn. Remove once CompilerParameters,
// CompilerResults, and CompilerError have been replaced with
// AWB-owned types.
global using System.CodeDom.Compiler;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net;
global using System.Net.Http;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading.Tasks;