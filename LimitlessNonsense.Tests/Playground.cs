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

            TimeOnly a = new TimeOnly(09, 0, 0);
            TimeOnly b = new TimeOnly(10, 0, 0);

            Console.WriteLine(a - b);
        }
    }
}
