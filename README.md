# SyslogKinesis
Receive Syslog Events and send to a Kinesis Stream or Firehose.

## Configuration
Settings can be applied using appsettings.json or environment variables. The stream name and stream type need to be set based on the stream to be published to in AWS Kinesis. The listening port is the TCP port that the syslog server listens on for syslog messages.

{
  "STREAMNAME": "example-kinesis-stream",
  "STREAMTYPE": "firehose",
  "LISTENINGPORT": 514 
}

## Example Messages
The Json below is an example of the data which will arrive in Kinesis when processing a syslog message.

{
    "Timestamp": "2020-06-29T09:20:09.6176152Z",
    "Level": "Information",
    "MessageTemplate": "{@SourceIp}: {@SyslogMessage}",
    "RenderedMessage": "\"172.12.0.1\": SyslogMessage { Facility: User, Severity: Notice, Datestamp: 06/29/2020 09:20:05, Content: \"ec2-user: sample message\", Host: \"ip-172-12-0-1\" }",
    "Properties": {
        "SourceIp": "172.12.0.1",
        "SyslogMessage": {
            "_typeTag": "SyslogMessage",
            "Facility": "User",
            "Severity": "Notice",
            "Datestamp": "2020-06-29T09:20:05.0000000",
            "Content": "ec2-user: sample message",
            "Host": "ip-172-12-0-1"
        }
    }
}
