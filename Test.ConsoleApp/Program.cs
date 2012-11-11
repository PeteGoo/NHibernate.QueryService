using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.ConsoleApp.NHibernateQueryService;

namespace Test.ConsoleApp {
    class Program {
        static void Main(string[] args) {
            QueryServiceContext context = new QueryServiceContext(new Uri("http://localhost:17104/QueryService.svc"));
            var people = context.People.Expand("Addresses").Where(p => p.FirstName == "Peter").ToArray();
            people.Select(p => string.Format("{0} {1}", p.FirstName, p.LastName)).ToList().ForEach(Console.WriteLine);
            Console.ReadKey(true);
        }
    }
}
