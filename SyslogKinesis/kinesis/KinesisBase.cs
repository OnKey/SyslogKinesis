using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Serilog;

namespace SyslogKinesis.kinesis
{
    /// <summary>
    /// Base class for Kinesis Firehose and Data Stream publishers
    /// </summary>
    public abstract class KinesisBase : BatchedPeriodicPublisher
    {
        private static readonly Random Jitterer = new Random((int) DateTime.Now.Ticks);

        private static readonly AsyncRetryPolicy RetryPolicy = Policy
            .Handle<PutRecordsException>()
            .WaitAndRetryAsync(4,
                // exponential back-off plus some jitter
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 150)
                                + TimeSpan.FromMilliseconds(Jitterer.Next(0, 100)));

        public override async Task PublishEvents(IEnumerable<Object> eventList)
        {
            var i = 0;
            var listsToUpload = this.SplitRecordsList(eventList);
            var tasks = new List<Task<PolicyResult<AmazonWebServiceResponse>>>();
            foreach (var list in listsToUpload)
            {
                if (list.Count > 0)
                {
                    // try writing to firehose but handle any transient rate limits
                    var result = RetryPolicy.ExecuteAndCaptureAsync(() => this.PutRecordBatch(list));
                    tasks.Add(result);
                    i += list.Count;
                }
            }

            await Task.WhenAll(tasks.Cast<Task>().ToArray());
            foreach (var t in tasks)
            {
                if (t.Result.Outcome == OutcomeType.Failure)
                {
                    Log.Error($"Error trying to write events to Kinesis. {t.Result.FinalException}");
                }
            }
        }

        protected abstract Task<AmazonWebServiceResponse> PutRecordBatch(List<KinesisRecord> list);

        /// <summary>
        /// Take a list of events and split into multiple lists of records.
        ///
        /// Each record list can have a max of 500 records and 4MB of string data
        /// </summary>
        /// <param name="eventList">list of events to split</param>
        /// <returns>split lists</returns>
        private IEnumerable<List<KinesisRecord>> SplitRecordsList(IEnumerable<object> eventList)
        {
            var currentList = new RecordList();

            foreach (var e in eventList)
            {
                var json = JsonConvert.SerializeObject(e) + '\n';
                var bytes = Encoding.UTF8.GetBytes(json);
                Log.Debug(json);

                // Create a new list if we've hit either size limit
                if (currentList.WillListBeTooLarge(bytes))
                {
                    yield return currentList.Records;
                    currentList = new RecordList();
                }

                currentList.AddItem(new KinesisRecord {Data = bytes, PartitionKey = bytes.GetHashCode().ToString()});
            }

            yield return currentList.Records;
        }

        protected internal class RecordList
        {
            public List<KinesisRecord> Records = new List<KinesisRecord>();
            public int SizeBytes = 0;

            public bool WillListBeTooLarge(byte[] newItem)
            {
                var listCountLimit = 500;
                var listSizeLimit = 1024 * 1024 * 4.0;

                if (this.SizeBytes + newItem.Length > listSizeLimit || this.Records.Count + 1 > listCountLimit)
                {
                    return true;
                }

                return false;
            }

            public void AddItem(KinesisRecord newItem)
            {
                this.Records.Add(newItem);
                this.SizeBytes += newItem.Data.Length;
            }
        }

        /// <summary>
        /// Abstracts differences between record types for Kinesis Firehose and Data Stream
        /// </summary>
        protected internal class KinesisRecord
        {
            public byte[] Data { get; set; }

            /// <summary>
            /// Used to partition records if target is a Kinesis Data Stream
            /// </summary>
            public string PartitionKey { get; set; }

            /// <summary>
            /// Record was processed by Kinesis - used to make sure we don't reprocess records multiple times when we retry failed batches
            /// </summary>
            public bool ProccessedSuccessfully { get; set; }
        }


        internal class PutRecordsException : Exception
        {
            public AmazonWebServiceResponse Response { get; }

            public PutRecordsException(AmazonWebServiceResponse r)
            {
                this.Response = r;
            }
        }
    }
}
