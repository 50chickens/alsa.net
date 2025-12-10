namespace Example.AlsaSanity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var sanityTester = new AlsaSanityTester("default");
            sanityTester.TestSanity();
        }
    }
}
