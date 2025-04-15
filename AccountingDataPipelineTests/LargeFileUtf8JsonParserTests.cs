using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using AccountingDataPipeline;
using AccountingDataPipeline.Parsing;
using NUnit.Framework.Legacy;

namespace AccountingDataPipeline.Tests
{
    [TestFixture]
    public class LargeFileUtf8JsonParserTests
    {
        // Test 1: Valid JSON array should produce the correct records.
        [Test]
        public async Task Parse_ValidJsonArray_ReturnsRecords()
        {
            // Arrange
            var expectedRecords = new List<Record>
            {
                new Record { Id = 1, Name = "A", Value = 1.1 },
                new Record { Id = 2, Name = "B", Value = 2.2 },
                new Record { Id = 3, Name = "C", Value = 3.3 }
            };
            string json = JsonSerializer.Serialize(expectedRecords);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var parser = new LargeFileUtf8JsonParser();

            // Act
            var actualRecords = new List<Record>();
            await foreach (var record in parser.ParseAsync<Record>(stream))
            {
                actualRecords.Add(record);
            }

            // Assert
            ClassicAssert.AreEqual(expectedRecords.Count, actualRecords.Count, "Record count mismatch");
            for (int i = 0; i < expectedRecords.Count; i++)
            {
                ClassicAssert.AreEqual(expectedRecords[i].Id, actualRecords[i].Id, $"Record {i} Id mismatch");
                ClassicAssert.AreEqual(expectedRecords[i].Name, actualRecords[i].Name, $"Record {i} Name mismatch");
                ClassicAssert.AreEqual(expectedRecords[i].Value, actualRecords[i].Value, $"Record {i} Value mismatch");
            }
        }

        // Test 2: When input is not a JSON array, a JsonException should be thrown.
        [Test]
        public void Parse_NotArrayInput_ThrowsJsonException()
        {
            // Arrange: Use an object instead of an array.
            string json = "{\"Id\":1,\"Name\":\"A\",\"Value\":1.1}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var parser = new LargeFileUtf8JsonParser();

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(async () =>
            {
                await foreach (var record in parser.ParseAsync<Record>(stream))
                {
                    // No operation needed; expecting exception.
                }
            });
        }

        // Test 3: Incomplete JSON should throw a JsonException.
        [Test]
        public void Parse_IncompleteJsonInput_ThrowsJsonException()
        {
            // Arrange: Truncated JSON array (missing closing bracket).
            string json = "[{\"Id\":1,\"Name\":\"A\",\"Value\":1.1}, {\"Id\":2,\"Name\":\"B\",\"Value\":2.2}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var parser = new LargeFileUtf8JsonParser();

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(async () =>
            {
                await foreach (var record in parser.ParseAsync<Record>(stream))
                {
                    // No operation; expecting exception.
                }
            });
        }

        // Test 4: JSON with extra commas and whitespace should still parse correctly.
        [Test]
        public async Task Parse_JsonArrayWithExtraCommasAndWhitespace_ReturnsRecords()
        {
            // Arrange: JSON array with stray commas and whitespace.
            string json = @"
            [
                { ""Id"": 1, ""Name"": ""A"", ""Value"": 1.1 },
                { ""Id"": 2, ""Name"": ""B"", ""Value"": 2.2 },
                { ""Id"": 3, ""Name"": ""C"", ""Value"": 3.3 }
                ,  
            ]";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var parser = new LargeFileUtf8JsonParser();

            // Act
            var records = new List<Record>();
            await foreach (var record in parser.ParseAsync<Record>(stream))
            {
                records.Add(record);
            }

            // Assert
            ClassicAssert.AreEqual(3, records.Count, "Expected 3 records");
            ClassicAssert.AreEqual(1, records[0].Id);
            ClassicAssert.AreEqual("A", records[0].Name);
            ClassicAssert.AreEqual(1.1, records[0].Value);
        }

        // Test 5: Use the generic fallback to parse a JSON array of integers.
        [Test]
        public async Task Parse_GenericFallback_ReturnsInts()
        {
            // Arrange: JSON array of integers.
            string json = "[1, 2, 3, 4, 5]";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var parser = new LargeFileUtf8JsonParser();

            // Act
            var numbers = new List<int>();
            await foreach (var num in parser.ParseAsync<int>(stream))
            {
                numbers.Add(num);
            }

            // Assert
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, numbers, "The parsed integers don't match expected values");
        }
    }
}
