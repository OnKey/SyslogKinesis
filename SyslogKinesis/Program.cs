using System;
using System.Threading.Tasks;
using Serilog;
using SyslogKinesis.kinesis;
using SyslogKinesis.syslog;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace SyslogKinesis
{
    class Program
    {
        private static String streamname;
        private static KinesisType streamtype;
        private static int listeningPort = 514;
        private static LoggingLevelSwitch LogLevel;

        static void Main(string[] args)
        {
            ConfigureLogging();
            Log.Information("Starting SyslogKinesis");
            GetConfiguration();

            var kinesisLogger = streamtype == KinesisType.Firehose ? KinesisLogFactory.GetKinesisFirehoseLogger(streamname) : KinesisLogFactory.GetKinesisStreamLogger(streamname);

            var handler = new TcpConnectionHandler(kinesisLogger);
            var tcpListener = new TcpServer(handler, listeningPort);
            var tcpTask = tcpListener.Run();

            var udpListener = new UdpServer(listeningPort, kinesisLogger);
            var udpTask = udpListener.Run();

            Task.WhenAny(tcpTask, udpTask).Wait(); // Stop if either server stops
        }

        static void ConfigureLogging()
        {
            LogLevel = new LoggingLevelSwitch();
            LogLevel.MinimumLevel = LogEventLevel.Information;
            var log = new LoggerConfiguration()
                .WriteTo.Console();
            log.MinimumLevel.ControlledBy(LogLevel);
            Log.Logger = log.CreateLogger();
        }

        static void GetConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            streamname = config["STREAMNAME"];
            if (string.IsNullOrEmpty(streamname))
            {
                throw new ArgumentException(
                    "STREAMNAME must be set in either an environment variable or appsettings.json with the name of the Kinesis stream/firehose to publish to");
            }

            if (string.Equals(config["STREAMTYPE"], "firehose", StringComparison.InvariantCultureIgnoreCase))
            {
                streamtype = KinesisType.Firehose;
            } 
            else if (string.Equals(config["STREAMTYPE"], "stream", StringComparison.InvariantCultureIgnoreCase))
            {
                streamtype = KinesisType.Stream;
            }
            else
            {
                throw new ArgumentException("STREAMTYPE must be set in either an environment variable or appsettings.json with firehose or stream");
            }
            Log.Information($"Sending events to {streamtype} - {streamname}");

            if (!string.IsNullOrEmpty(config["LISTENINGPORT"]))
            {
                Log.Information($"Listening on port {config["LISTENINGPORT"]}");
                listeningPort = int.Parse(config["LISTENINGPORT"]);
            }

            if (!string.IsNullOrEmpty(config["AWSPROFILE"]))
            {
                Log.Information($"Using AWS Profile: {config["AWSPROFILE"]}");
                Environment.SetEnvironmentVariable("AWS_PROFILE", config["AWSPROFILE"]);
            }

            if (!string.IsNullOrEmpty(config["LOGLEVEL"]))
            {
                switch (config["LOGLEVEL"].ToLower())
                {
                    case "information":
                        Log.Information("Setting log level to: Information");
                        LogLevel.MinimumLevel = LogEventLevel.Information;
                        break;
                    case "debug":
                        Log.Information("Setting log level to: Debug");
                        LogLevel.MinimumLevel = LogEventLevel.Debug;
                        break;
                    case "warning":
                        Log.Information("Setting log level to: Warning");
                        LogLevel.MinimumLevel = LogEventLevel.Warning;
                        break;
                    case "error":
                        Log.Information("Setting log level to: Error");
                        LogLevel.MinimumLevel = LogEventLevel.Error;
                        break;
                }
            }
        }

        enum KinesisType
        {
            Firehose, Stream
        }
    }
}
