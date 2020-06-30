using System;
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
            var syslog = new SyslogMessage(msg);

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
            var syslog = new SyslogMessage(msg);

            Assert.AreEqual(SyslogMessage.FacilityType.Auth, syslog.Facility);
            Assert.AreEqual(SyslogMessage.SeverityType.Critical, syslog.Severity);
            Assert.AreEqual(DateTime.ParseExact("Oct 11 22:14:15", "MMM dd HH:mm:ss", null), syslog.Datestamp);
            Assert.AreEqual("testhost", syslog.Host);
            Assert.AreEqual("smtp: failed to send email", syslog.Content);
        }
    }
}