/////////////////////////////////////////////////////////////////////
// CodeToTest1.cs - define code to be tested                       //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadingTests
{
  public class CodeToTest1
  {
    public void annunciator()
    {
            int a = 2;
            int b = 3;

      Console.Write("\n  Result pf multiplication of {0} and {1} is {2}",a,b,a*b);
    }
    static void Main(string[] args)
    {
      CodeToTest1 code = new CodeToTest1();
      code.annunciator();
      Console.Write("\n\n");
    }
  }
}
