using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    public class Assembler
    {
        private const int WORD_SIZE = 16;

        private Dictionary<string, int[]> m_dControl, m_dJmp; //these dictionaries map command mnemonics to machine code - they are initialized at the bottom of the class

        //more data structures here (symbol map, ...)
       
        private Dictionary<string, int> SymbolTable,DoubleLable;

        public Assembler()
        {
            InitCommandDictionaries();
        }

        //this method is called from the outside to run the assembler translation
        public void TranslateAssemblyFile(string sInputAssemblyFile, string sOutputMachineCodeFile)
        {
            //read the raw input, including comments, errors, ...
            StreamReader sr = new StreamReader(sInputAssemblyFile);
            List<string> lLines = new List<string>();
            while (!sr.EndOfStream)
            {
                lLines.Add(sr.ReadLine());
            }
            sr.Close();
            //translate to machine code
            List<string> lTranslated = TranslateAssemblyFile(lLines);
            //write the output to the machine code file
            StreamWriter sw = new StreamWriter(sOutputMachineCodeFile);
            foreach (string sLine in lTranslated)
                sw.WriteLine(sLine);
            sw.Close();
        }

        //translate assembly into machine code
        private List<string> TranslateAssemblyFile(List<string> lLines)
        {
            //implementation order:
            //first, implement "TranslateAssemblyToMachineCode", and check if the examples "Add", "MaxL" are translated correctly.
            //next, implement "CreateSymbolTable", and modify the method "TranslateAssemblyToMachineCode" so it will support symbols (translating symbols to numbers). check this on the examples that don't contain macros
            //the last thing you need to do, is to implement "ExpendMacro", and test it on the example: "SquareMacro.asm".
            //init data structures here 

            //expand the macros
            List<string> lAfterMacroExpansion = ExpendMacros(lLines);

            //first pass - create symbol table and remove lable lines
            CreateSymbolTable(lAfterMacroExpansion);

            //second pass - replace symbols with numbers, and translate to machine code
            List<string> lAfterTranslation = TranslateAssemblyToMachineCode(lAfterMacroExpansion);
            return lAfterTranslation;
        }

        
        //first pass - replace all macros with real assembly
        private List<string> ExpendMacros(List<string> lLines)
        {
            //You do not need to change this function, you only need to implement the "ExapndMacro" method (that gets a single line == string)
            List<string> lAfterExpansion = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                //remove all redudant characters
                string sLine = CleanWhiteSpacesAndComments(lLines[i]);
                if (sLine == "")
                    continue;
                //if the line contains a macro, expand it, otherwise the line remains the same
                List<string> lExpanded = ExapndMacro(sLine);
                //we may get multiple lines from a macro expansion
                foreach (string sExpanded in lExpanded)
                {
                    lAfterExpansion.Add(sExpanded);
                }
            }
            return lAfterExpansion;
        }

        //expand a single macro line
        private List<string> ExapndMacro(string sLine)
        {
            List<string> lExpanded = new List<string>();
            int number = 0;
            if (IsCCommand(sLine))
            {
                string sDest, sCompute, sJmp;
                GetCommandParts(sLine, out sDest, out sCompute, out sJmp);
                if(sCompute.Contains("++"))
                {
                     String Label = sCompute.Substring(0, sCompute.IndexOf('+'));
                    if (Label.Contains("D") || Label.Contains("A") || Label.Contains("M"))
                    {
                         String Line = Label + "=" + Label + "+1";
                        lExpanded.Add(Line);
                    }
                    else
                    {
                     String Line1 = "@" + Label;
                     String Line2 = "M=M+1";

                     lExpanded.Add(Line1);
                     lExpanded.Add(Line2);
                    }
                }
                else if(sCompute.Contains("--"))
                {
                     String Label = sCompute.Substring(0, sCompute.IndexOf('-'));
                    if (Label.Contains("D") || Label.Contains("A") || Label.Contains("M"))
                    {
                         String Line = Label + "=" + Label + "-1";
                        lExpanded.Add(Line);
                    }
                    else 
                    {
                     String Line1 = "@" + Label;
                     String Line2 = "M=M-1";

                     lExpanded.Add(Line1);
                     lExpanded.Add(Line2);
                    }
                }
              else if (sLine.Contains("=") && !sDest.Contains("A") && !sDest.Contains("D") && !sDest.Contains("M") && !sCompute.Contains("A") && !sCompute.Contains("D") && !sCompute.Contains("M")&& !Int32.TryParse(sCompute , out number ))
                {
                    String Line1 = "@" + sCompute;
                    String Line2 = "D=M";
                    String Line3 = "@" + sDest;
                    String Line4 = "M=D";
                    lExpanded.Add(Line1);
                    lExpanded.Add(Line2);
                    lExpanded.Add(Line3);
                    lExpanded.Add(Line4);
                }
                else if (Int32.TryParse(sCompute , out number )  && !m_dControl.ContainsKey(sCompute) && sLine.Contains('='))
                {
                    if (sDest.Contains("A") || sDest.Contains("D") || sDest.Contains("M"))
                    {
                        String Line1 = "@" + sCompute;
                        String Line2 = sDest + "=A";

                        lExpanded.Add(Line1);
                        lExpanded.Add(Line2);
                    }
                    else
                    {
                        String Line1 = "@" + sCompute;
                        String Line2 = "D=A";
                        String Line3 = "@" + sDest;
                        String Line4 = "M=D";

                        lExpanded.Add(Line1);
                        lExpanded.Add(Line2);
                        lExpanded.Add(Line3);
                        lExpanded.Add(Line4);
                    }
                }
                else if (sLine.Contains('=') && !sDest.Contains("A") && !sDest.Contains("D") && !sDest.Contains("M") && (sCompute.Contains("A") || sCompute.Contains("D") || sCompute.Contains("M") || sCompute.Contains("0") || sCompute.Contains("1")))
                {
                    String Line1 = "@" + sDest;
                    String Line2 = "M=" + sCompute;

                    lExpanded.Add(Line1);
                    lExpanded.Add(Line2);
                }
    
                else if (sLine.Contains("=") && (sDest.Contains("A") || sDest.Contains("D") || sDest.Contains("M")) && !sCompute.Contains("A") && !sCompute.Contains("D") && !sCompute.Contains("M") && !Int32.TryParse(sCompute , out number ) )
                {
                    String Line1 = "@" + sCompute;
                    String Line2 = sDest + "=M";

                    lExpanded.Add(Line1);
                    lExpanded.Add(Line2);
                }
     
                 else if (sLine.Contains(':'))
                {
                    String jump = sJmp.Substring(0, sJmp.IndexOf(':'));
                    String label = sJmp.Substring(sJmp.IndexOf(':') + 1);

                    String Line1 = "@" + label;
                    String Line2 = sCompute + ";" + jump;

                    lExpanded.Add(Line1);
                    lExpanded.Add(Line2);
                }
            }
            if (lExpanded.Count == 0)
                lExpanded.Add(sLine);
            return lExpanded;
        }

        //second pass - record all symbols - labels and variables
        private void CreateSymbolTable(List<string> lLines)
        {
            string sLine = "";

          //  int indexOfTable = 16;

            SymbolTable = new Dictionary<string, int>();
            SymbolTable["R0"] = 0;
            SymbolTable["R1"] = 1;
            SymbolTable["R2"] = 2;
            SymbolTable["R3"] = 3;
            SymbolTable["R4"] = 4;
            SymbolTable["R5"] = 5;
            SymbolTable["R6"] = 6;
            SymbolTable["R7"] = 7;
            SymbolTable["R8"] = 8;
            SymbolTable["R9"] = 9;
            SymbolTable["R10"] = 10;
            SymbolTable["R11"] = 11;
            SymbolTable["R12"] = 12;
            SymbolTable["R13"] = 13;
            SymbolTable["R14"] = 14;
            SymbolTable["R15"] = 15;
            SymbolTable["SCREEN"] = 16384;
            DoubleLable = new Dictionary<string, int>();
            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];
                if (IsLabelLine(sLine))
                {
                    String Label = sLine.Substring(1, sLine.Length - 2);

                    if (Label[0] <= '9' && Label[0] >= '0' )
                    {
                        throw new ArgumentException("Label Cannot Start With Number"); 
                    }
     
                    if ( SymbolTable.ContainsKey(Label) )
                    {
                      
                        if (!DoubleLable.ContainsKey(Label))
                        {
                           SymbolTable[Label] = i;
                           DoubleLable[Label] = 1;
                           lLines.Remove(lLines[i]);
                           i--;

                           
                        }
                        else
                        {
                            throw new ArgumentException("Double Label Exist"); //exception
                        }
                    }
                    else
                    {
                        //we figure it's a label that doesnt exists in our symbol map
                       SymbolTable[Label] = i;
                       DoubleLable[Label] = 1;
                        lLines.Remove(lLines[i]);
                        i--;
             
                      
                    }
                }
                else if (IsACommand(sLine))
                {
                    int number=0;
                    String Label = sLine.Substring(1);
                    if (!Int32.TryParse(Label, out number)) 
                    {
                        if (sLine[1] <= '9' && sLine[1] >= '0') 
                        {
                            throw new ArgumentException("Label Cannot Strart With Number"); 
                        }

                        if (!SymbolTable.ContainsKey(sLine.Substring(1, sLine.Length - 1)))
                        {
                            SymbolTable[sLine.Substring(1, sLine.Length - 1)] = -1;
                            
                        }
                        
                    }
                    
                }
                    //may contain a variable - if so, record it to the symbol table (if it doesn't exist there yet...)
               
                else if (IsCCommand(sLine))
                {
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
       
        }
        
        //third pass - translate lines into machine code, replacing symbols with numbers
        private List<string> TranslateAssemblyToMachineCode(List<string> lLines)
        {
            int indexOfTable=16;
            int [] Dest = new int[3];
            string sLine = "";
            List<string> lAfterPass = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];
                if (IsACommand(sLine))
                {
                   String line =sLine.Substring(1);
                    int Number;
                    if (Int32.TryParse(line, out Number))
                    {
                        lAfterPass.Add(ToBinary(Number));
                    }
                    else
                    {
                        if ( SymbolTable.ContainsKey(line))
                            if(SymbolTable[line]==-1)
                            {
                                SymbolTable[line]=indexOfTable;
                                indexOfTable++;
                            }
                            lAfterPass.Add(ToBinary(SymbolTable[line]));
                    }
                }
                else if (IsCCommand(sLine))
                {
                    string sDest, sControl, sJmp;
                    GetCommandParts(sLine, out sDest, out sControl, out sJmp);
                    //translate an C command into a sequence of bits
                    //take a look at the dictionaries m_dControl, m_dJmp, and where they are initialized (InitCommandDictionaries), to understand how to you them here
                    if (!m_dControl.ContainsKey(sControl))
                        throw new ArgumentException("Not legal source"); 
                    int[] Control = m_dControl[sControl];
                     if (!m_dJmp.ContainsKey(sJmp))
                        throw new ArgumentException("Not legal Jump"); 
                    int[] Jmp = m_dJmp[sJmp];

                    if (sDest.Contains("A"))
                     {
                        Dest[0]=1;
                        Dest[1]=0;
                        Dest[2]=0;
                    }
                    else if (sDest.Contains("M"))
                    {
                        Dest[0]=0;
                        Dest[1]=0;
                        Dest[2]=1;
                    }
                    else if (sDest.Contains("D"))
                    {
                        Dest[0]=0;
                        Dest[1]=1;
                        Dest[2]=0;
                    }
                    else
                     {
                        Dest[0]=0;
                        Dest[1]=0;
                        Dest[2]=0;
                    }
                      

                    String assemblyComand = "100" + ToString(Control) + ToString(Dest) + ToString(Jmp);

                    lAfterPass.Add(assemblyComand);
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
            SymbolTable.Clear();
            return lAfterPass;
        }

        //helper functions for translating numbers or bits into strings
        private string ToString(int[] aBits)
        {
            string sBinary = "";
            for (int i = 0; i < aBits.Length; i++)
                sBinary += aBits[i];
            return sBinary;
        }

        private string ToBinary(int x)
        {
            string sBinary = "";
            for (int i = 0; i < WORD_SIZE; i++)
            {
                sBinary = (x % 2) + sBinary;
                x = x / 2;
            }
            return sBinary;
        }


        //helper function for splitting the various fields of a C command
        private void GetCommandParts(string sLine, out string sDest, out string sControl, out string sJmp)
        {
            if (sLine.Contains('='))
            {
                int idx = sLine.IndexOf('=');
                sDest = sLine.Substring(0, idx);
                sLine = sLine.Substring(idx + 1);
            }
            else
                sDest = "";
            if (sLine.Contains(';'))
            {
                int idx = sLine.IndexOf(';');
                sControl = sLine.Substring(0, idx);
                sJmp = sLine.Substring(idx + 1);

            }
            else
            {
                sControl = sLine;
                sJmp = "";
            }
        }

        private bool IsCCommand(string sLine)
        {
            return !IsLabelLine(sLine) && sLine[0] != '@';
        }

        private bool IsACommand(string sLine)
        {
            return sLine[0] == '@';
        }

        private bool IsLabelLine(string sLine)
        {
            if (sLine.StartsWith("(") && sLine.EndsWith(")"))
                return true;
            return false;
        }

        private string CleanWhiteSpacesAndComments(string sDirty)
        {
            string sClean = "";
            for (int i = 0 ; i < sDirty.Length ; i++)
            {
                char c = sDirty[i];
                if (c == '/' && i < sDirty.Length - 1 && sDirty[i + 1] == '/') // this is a comment
                    return sClean;
                if (c > ' ' && c <= '~')//ignore white spaces
                    sClean += c;
            }
            return sClean;
        }


        private void InitCommandDictionaries()
        {
            m_dControl = new Dictionary<string, int[]>();

            m_dControl["0"] = new int[] { 0, 1, 0, 1, 0, 1, 0 };
            m_dControl["1"] = new int[] { 0, 1, 1, 1, 1, 1, 1 };
            m_dControl["-1"] = new int[] { 0, 1, 1, 1, 0, 1, 0 };
            m_dControl["D"] = new int[] { 0, 0, 0, 1, 1, 0, 0 };
            m_dControl["A"] = new int[] { 0, 1, 1, 0, 0, 0, 0 };
            m_dControl["!D"] = new int[] { 0, 0, 0, 1, 1, 0, 1 };
            m_dControl["!A"] = new int[] { 0, 1, 1, 0, 0, 0, 1 };
            m_dControl["-D"] = new int[] { 0, 0, 0, 1, 1, 1, 1 };
            m_dControl["-A"] = new int[] { 0, 1, 1, 0, 0,1, 1 };
            m_dControl["D+1"] = new int[] { 0, 0, 1, 1, 1, 1, 1 };
            m_dControl["A+1"] = new int[] { 0, 1, 1, 0, 1, 1, 1 };
            m_dControl["D-1"] = new int[] { 0, 0, 0, 1, 1, 1, 0 };
            m_dControl["A-1"] = new int[] { 0, 1, 1, 0, 0, 1, 0 };
            m_dControl["D+A"] = new int[] { 0, 0, 0, 0, 0, 1, 0 };
            m_dControl["D-A"] = new int[] { 0, 0, 1, 0, 0, 1, 1 };
            m_dControl["A-D"] = new int[] { 0, 0, 0, 0, 1,1, 1 };
            m_dControl["D&A"] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            m_dControl["D|A"] = new int[] { 0, 0, 1, 0,1, 0, 1 };

            m_dControl["M"] = new int[] { 1, 1, 1, 0, 0, 0, 0 };
            m_dControl["!M"] = new int[] { 1, 1, 1, 0, 0, 0, 1 };
            m_dControl["-M"] = new int[] { 1, 1, 1, 0, 0, 1, 1 };
            m_dControl["M+1"] = new int[] { 1, 1, 1, 0, 1, 1, 1 };
            m_dControl["M-1"] = new int[] { 1, 1, 1, 0, 0, 1, 0 };
            m_dControl["D+M"] = new int[] { 1, 0, 0, 0, 0, 1, 0 };
            m_dControl["D-M"] = new int[] { 1, 0, 1, 0, 0, 1, 1 };
            m_dControl["M-D"] = new int[] { 1, 0, 0, 0, 1, 1, 1 };
            m_dControl["D&M"] = new int[] { 1, 0, 0, 0, 0, 0, 0 };
            m_dControl["D|M"] = new int[] { 1, 0, 1, 0, 1, 0, 1 };
            m_dControl["M+D"] = new int[] { 1, 0, 0, 0, 0, 1, 0 };


            m_dJmp = new Dictionary<string, int[]>();

            m_dJmp[""] = new int[] { 0, 0, 0 };
            m_dJmp["JGT"] = new int[] { 0, 0, 1 };
            m_dJmp["JEQ"] = new int[] { 0, 1, 0 };
            m_dJmp["JGE"] = new int[] { 0, 1, 1 };
            m_dJmp["JLT"] = new int[] { 1, 0, 0 };
            m_dJmp["JNE"] = new int[] { 1, 0, 1 };
            m_dJmp["JLE"] = new int[] { 1, 1, 0 };
            m_dJmp["JMP"] = new int[] { 1, 1, 1 };

            
        }
    }
}
