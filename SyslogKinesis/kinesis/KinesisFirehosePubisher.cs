using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.Runtime;
using Serilog;

namespace SyslogKinesis.kinesis
{
    /// <summary>
    /// Writes events to Kinesis Firehose.
    ///
    /// Relies on AWS profile being set up on local machine for authentication to AWS.
    /// </summary>
    public class KinesisFirehosePubisher : KinesisBase
    {
        IAmazonKinesisFirehose _client;
        string _deliveryStreamName;

        public KinesisFirehosePubisher(string deliveryStreamName)
        {
            this._client = new AmazonKinesisFirehoseClient();
            this._deliveryStreamName = deliveryStreamName;
        }

        public override void Dispose()
        {
            this._client.Dispose();
        }

        protected override async Task<AmazonWebServiceResponse> PutRecordBatch(List<KinesisRecord> input)
        {
            var records = this.GetRecordList(input);
            var request = new PutRecordBatchRequest
            {
                DeliveryStreamName = this._deliveryStreamName,
                Records = records
            };

            var response = await this._client.PutRecordBatchAsync(request);
            if (response.HttpStatusCode != HttpStatusCode.OK || response.FailedPutCount > 0)
            {
                var errorList = response.RequestResponses.Where(x => x.ErrorCode != null)
                    .Select(x => x.ErrorCode + ":" + x.ErrorMessage).Distinct();
                var errors = string.Join(", ", errorList);
                Log.Warning($"Transient fault trying to write {response.FailedPutCount} events to Kinesis. Response: {response.HttpStatusCode} {errors}");
                throw new PutRecordsException(response);
            }

            return response;
        }

        private List<Record> GetRecordList(List<KinesisRecord> list)
        {
            var result = new List<Record>();
            foreach (var record in list)
            {
                var r = new Record
                {
                    Data = new MemoryStream(record.Data)
                };
                result.Add(r);
            }

            return result;
        }
    }
}
