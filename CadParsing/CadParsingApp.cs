using System;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CadParsing.CadParsingApp))]

namespace CadParsing
{
    public class CadParsingApp : IExtensionApplication
    {
        public void Initialize()
        {
            Console.WriteLine("CadParsing plugin initialized.");
        }

        public void Terminate() { }
    }
}
