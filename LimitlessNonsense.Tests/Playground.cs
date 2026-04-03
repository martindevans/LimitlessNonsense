using System;
using System.Collections.Generic;
using System.Text;

namespace LimitlessNonsense.Tests
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void Foo()
        {
            var d = DateTime.UtcNow;
            var o = (object)d;

            Console.WriteLine($"{d:d}");
            Console.WriteLine($"{o:d}");
        }
    }
}
