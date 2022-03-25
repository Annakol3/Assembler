using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            Assembler a = new Assembler();
            //to run tests, call the "TranslateAssemblyFile" function like this:
            //string sourceFileLocation = the path to your source file
            //string destFileLocation = the path to your dest file
            //a.TranslateAssemblyFile(sourceFileLocation, destFileLocation);
            a.TranslateAssemblyFile(@"C:\Users\annak\Desktop\Assembly examples\Add.asm", @"C:\Users\annak\Desktop\Assembly examples\Add.hack");
            //You need to be able to run two translations one after the other
         //   a.TranslateAssemblyFile(@"Max.asm", @"Max.hack");
        }
    }
}
