using System;
using System.Linq;
using System.Threading.Tasks;
using ConsoleApp_EFCore_Oracle;
using Microsoft.EntityFrameworkCore;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation.PredefinedTransformations;

namespace ConsoleApp_EFCore_SqlGeneration
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var db = new TestContext())
            {
                //await db.Database.EnsureDeletedAsync();

                await db.Database.EnsureCreatedAsync();

                var entity = db.XhhTests.Add(new xhhTest()
                {
                    Txt = Guid.NewGuid().ToString()
                }).Entity;

                var id = entity.Id;

                db.SaveChanges();


                var res_XhhTest = await db.XhhTests.ToListAsync();

                db.XhhTests.Remove(res_XhhTest.FirstOrDefault());

                db.SaveChanges();
            }

            Console.ReadLine();
            Console.WriteLine("Hello World!");
        }
    }
}
