using CircuitBreakerExperiment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    public class Program
    {
        private static readonly CircuitBreaker myCircuitBreaker = new CircuitBreaker(3, TimeSpan.FromMinutes(15));
        static void Main(string[] args)
        {
        TestCircuitBreakerClass test = new TestCircuitBreakerClass();
            Console.WriteLine(test.TestCircuitBreaker());
            Console.WriteLine(test.TestCircuitBreaker());
            Console.WriteLine(test.TestCircuitBreaker());

            Console.ReadLine();
        }

        public class TestCircuitBreakerClass
        {

            public string TestCircuitBreaker()
            {
                if (myCircuitBreaker.AttemptCall(() => { DoSomething(); }).IsClosed)
                {
                    return "Called Code Successfully";
                }
                else
                {
                    return "too many fail attempts";
                }
            }

            public void DoSomething()
            {
                for (int i = 0; i < 5; i++)
                {
                    Console.Write("DoSomething");
                    throw new NotImplementedException();
                }
            }


        }
    }


}
