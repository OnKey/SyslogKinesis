using System;
using Amazon;
using Amazon.Kinesis;
using Amazon.KinesisFirehose;
using Serilog;
using Serilog.Formatting.Display;
using Serilog.Sinks.Amazon.Kinesis.Common;
using Serilog.Sinks.Amazon.Kinesis.Firehose;
using Serilog.Sinks.Amazon.Kinesis.Stream;

namespace SyslogKinesis.kinesis
{
    class KinesisLogFactory
    {
        /// <summary>
        /// Set up a Serilog logger to a Kinesis Stream. Will try to create the stream if it doesn't exist already.
        /// </summary>
        /// <param name="streamName">Name of the stream to write to</param>
        /// <param name="shardCount">Number of shards. Only needed when creating new streams.</param>
        /// <returns></returns>
        public static ILogger GetKinesisStreamLogger(string streamName, int shardCount = 0)
        {
            var client = new AmazonKinesisClient();
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug();

            loggerConfig.WriteTo.AmazonKinesis(
                kinesisClient: client,
                streamName: streamName,
                period: TimeSpan.FromSeconds(2),
                bufferBaseFilename: "./logs/kinesis-buffer",
                onLogSendError: OnLogSendError
            );

            return loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Set up a Serilog logger to a Kinesis Stream. Will try to create the stream if it doesn't exist already.
        /// </summary>
        /// <param name="streamName">Name of the stream to write to</param>
        /// <param name="shardCount">Number of shards. Only needed when creating new streams.</param>
        /// <returns></returns>
        public static ILogger GetKinesisFirehoseLogger(string streamName)
        {
            var client = new AmazonKinesisFirehoseClient();
            
            // temp
            Serilog.Debugging.SelfLog.Enable(Console.Error);
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug();

            loggerConfig.WriteTo.AmazonKinesisFirehose(
                kinesisFirehoseClient: client,
                streamName: streamName,
                period: TimeSpan.FromSeconds(2),
                bufferBaseFilename: "./logs/kinesis-buffer",
                onLogSendError: OnLogSendError
            );

            return loggerConfig.CreateLogger();
        }

        static void OnLogSendError(object sender, LogSendErrorEventArgs logSendErrorEventArgs)
        {
            Log.Error(logSendErrorEventArgs.Message);
        }
    }
}
