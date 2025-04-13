using AccountingDataPipeline.Processing;

namespace AccountingDataPipeline.Extensions
{
    public static class BatchExtensions
    {
        public static async IAsyncEnumerable<List<TOutput>> TransformAndBatchAsync<TInput, TOutput>(
            this IAsyncEnumerable<TInput> source,
            IRecordTransformer<TInput, TOutput> transformer,
            int batchSize,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batch = new List<TOutput>(batchSize);
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                var transformed = transformer.Transform(item);
                batch.Add(transformed);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<TOutput>(batchSize);
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }

        public static async IAsyncEnumerable<List<T>> BatchAsync<T>(
            this IAsyncEnumerable<T> source,
            int batchSize,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(batchSize);
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}