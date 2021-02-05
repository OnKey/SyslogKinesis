using System;
using System.Globalization;
using NUnit.Framework;
using SyslogKinesis.syslog;

namespace SyslogKinesisTest
{
    public class SyslogMessageTest
    {
        [Test]
        public void CanParseRfc5424()
        {
            var msg = @"<34>1 2003-10-11T22:14:15.003Z testhost.example.com smtp - failed to send email";
            var syslog = new SyslogMessage(msg, "127.0.0.1");

            Assert.AreEqual(SyslogMessage.FacilityType.Auth, syslog.Facility);
            Assert.AreEqual(SyslogMessage.SeverityType.Critical, syslog.Severity);
            Assert.AreEqual(DateTime.Parse("2003-10-11T22:14:15.003Z", null), syslog.Datestamp);
            Assert.AreEqual("testhost.example.com", syslog.Host);
            Assert.AreEqual("smtp - failed to send email", syslog.Content);
        }

        [Test]
        public void CanParseRfc3164()
        {
            var msg = @"<34>Oct 11 22:14:15 testhost smtp: failed to send email";
            var syslog = new SyslogMessage(msg, "127.0.0.1");

            Assert.AreEqual(SyslogMessage.FacilityType.Auth, syslog.Facility);
            Assert.AreEqual(SyslogMessage.SeverityType.Critical, syslog.Severity);
            Assert.AreEqual(DateTime.ParseExact("Oct 11 22:14:15", "MMM dd HH:mm:ss", null), syslog.Datestamp);
            Assert.AreEqual("testhost", syslog.Host);
            Assert.AreEqual("smtp: failed to send email", syslog.Content);
        }

        [Test]
        public void CanParseRfc3164DateEdgeCase()
        {
            var msg = @"<133>Jul  1 13:27:24 server1 abc: test msg";
            var syslog = new SyslogMessage(msg, "127.0.0.1");

            Assert.AreEqual(DateTime.ParseExact("Jul  1 13:27:24", "MMM d HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces), syslog.Datestamp);
        }

        [Test]
        public void CanParseCEF()
        {
            var msg = "<46>CEF:0|Device Vendor|Device Product|Device Version|Signature ID|Name|Severity|Extension";
            var syslog = new SyslogMessage(msg, "127.0.0.1");

            Assert.AreEqual(SyslogMessage.FacilityType.Syslog, syslog.Facility);
            Assert.AreEqual(SyslogMessage.SeverityType.Informational, syslog.Severity);
            Assert.AreEqual("127.0.0.1", syslog.SourceIp);
            Assert.AreEqual(msg.Substring(4), syslog.Content);
        }
    }
}