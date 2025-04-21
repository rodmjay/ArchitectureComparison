using System;
using System.Globalization;
using System.IO;
using Bogus;
using CsvHelper;

namespace CsvPerfGen
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // 1) parse record count
            var count = 1000;
            if (args.Length > 0 && int.TryParse(args[0], out var n) && n > 0)
                count = n;

            // 2) make Faker for Person
            var faker = new Faker<Person>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.EmailAddress, (f, p) =>
                    f.Internet.Email(p.FirstName, p.LastName));

            // 3) generate
            var records = faker.Generate(count);

            // 4) write CSV to output.txt
            using var writer = new StreamWriter("output.txt");
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(records);

            Console.WriteLine($"Wrote {count:N0} records to output.txt");
        }
    }
}