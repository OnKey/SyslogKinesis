namespace SyslogKinesis.kinesis
{
    public class KinesisLogFactory
    {
        public enum KinesisType
        {
            Firehose, Stream
        }

        public static IEventPublisher GetKinesisEventPublisher(KinesisType type, string streamName)
        {
            return type == KinesisType.Firehose
                ? (IEventPublisher)new KinesisFirehosePubisher(streamName)
                : (IEventPublisher)new KinesisDataStreamPublisher(streamName);
        }
    }
}
