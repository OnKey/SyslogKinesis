using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Serilog;

namespace SyslogKinesis.kinesis
{
    public class KinesisDataStreamPublisher : KinesisBase
    {
        private readonly string deliveryStreamName;
        private readonly AmazonKinesisClient client;

        public KinesisDataStreamPublisher(string deliveryStreamName)
        {
            this.client = new AmazonKinesisClient();
            this.deliveryStreamName = deliveryStreamName;
        }

        public override void Dispose()
        {
            this.client.Dispose();
        }

        protected override async Task<AmazonWebServiceResponse> PutRecordBatch(List<KinesisRecord> input)
        {
            var records = this.GetRecordList(input);
            var request = new PutRecordsRequest
            {
                StreamName = this.deliveryStreamName,
                Records = records
            };

            Log.Debug($"Sending {request.Records.Count} records to Kinesis");
            var response = await this.client.PutRecordsAsync(request);
            this.RecordSuccessfullyProcessedEvents(response, input);
            if (response.HttpStatusCode != HttpStatusCode.OK || response.FailedRecordCount > 0)
            {
                this.HandleErrors(response);
            }

            return response;
        }

        private void RecordSuccessfullyProcessedEvents(PutRecordsResponse response, List<KinesisRecord> input)
        {
            for (var i = 0; i < response.Records.Count; i++)
            {
                var record = input[i];
                var result = response.Records[i];
                if (string.IsNullOrEmpty(result.ErrorCode))
                {
                    record.ProccessedSuccessfully = true;
                }
            }
        }

        private void HandleErrors(PutRecordsResponse response)
        {
            var errorList = response.Records.Where(x => x.ErrorCode != null).Select(x => x.ErrorCode + ":" + x.ErrorMessage).Distinct();
            var errors = string.Join(", ", errorList);
            Log.Warning($"Transient fault trying to write {response.FailedRecordCount} events to Kinesis Data Stream. Response: {response.HttpStatusCode} {errors}");
            throw new PutRecordsException(response);
        }

        private List<PutRecordsRequestEntry> GetRecordList(List<KinesisRecord> list)
        {
            var result = new List<PutRecordsRequestEntry>();
            foreach (var record in list)
            {
                // This must be a retry, so don't reprocess records which have already been uploaded successfully
                if (record.ProccessedSuccessfully)
                {
                    continue;
                }

                var r = new PutRecordsRequestEntry
                {
                    Data = new MemoryStream(record.Data),
                    PartitionKey = record.PartitionKey
                };
                result.Add(r);
            }

            return result;
        }
    }
}
