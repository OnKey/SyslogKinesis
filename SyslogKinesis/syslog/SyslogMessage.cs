using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace SyslogKinesis.syslog
{
    public class SyslogMessage
    {
        private static Regex regex_rfc5424 = new Regex(@"^(\<\d{1,3}\>)\d\s(?:(\d{4}[-]\d{2}[-]\d{2}[T]\d{2}[:]\d{2}[:]\d{2}(?:\.\d{1,6})?(?:[+-]\d{2}[:]\d{2}|Z)?)|-)\s(?:([\w][\w\d\.@-]*)|-)\s(.*)$", RegexOptions.Compiled);
        private static Regex msg_rfc3164 = new Regex(@"^(\<\d{1,3}\>)([A-Z][a-z][a-z]\s{1,2}\d{1,2}\s\d{2}[:]\d{2}[:]\d{2})\s([\w][\w\d\.@-]*)\s(.*)$", RegexOptions.Compiled);

        [JsonConverter(typeof(StringEnumConverter))]
        public FacilityType Facility { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SeverityType Severity { get; set; }
        public DateTime Datestamp { get; set; }
        public string Content { get; set; }
        public string Host { get; set; }
        public string SoureIp { get; set; }

        public SyslogMessage(string rawMessage, string sourceIp)
        {
            this.SoureIp = sourceIp;

            var match = msg_rfc3164.Match(rawMessage);
            if (match.Success)
            {
                this.ReadRfc3164(match);
                return;
            }

            match = regex_rfc5424.Match(rawMessage);
            if (match.Success)
            {
                this.ReadRfc5424(match);
                return;
            }

            Log.Warning($"Message does not match RFC3164 or RFC5424 formats: {rawMessage}");
            throw new FormatException();
        }

        private void ReadRfc5424(Match match)
        {
            var pri = match.Groups[1].Value;
            var priority = int.Parse(pri.Substring(1, pri.Length - 2));
            this.Facility = (FacilityType)Math.Floor((double)priority / 8);
            this.Severity = (SeverityType)(priority % 8);

            var date = match.Groups[2].Value.TrimEnd();
            this.Datestamp = DateTime.Parse(date, null);

            this.Host = match.Groups[3].Value;

            this.Content = match.Groups[4].Value;
        }

        private void ReadRfc3164(Match match)
        {
            if (!match.Success)
            {
                throw new ArgumentException("Invalid syslog message");
            }

            var pri = match.Groups[1].Value;
            var priority = int.Parse(pri.Substring(1, pri.Length - 2));
            this.Facility = (FacilityType)Math.Floor((double)priority / 8);
            this.Severity = (SeverityType)(priority % 8);
           
            // rfc3164 has an odd date style which sometimes includes extra spaces, hence the parse config below
            var date = match.Groups[2].Value.TrimEnd();
            this.Datestamp = DateTime.ParseExact(date, "MMM d HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

            this.Host = match.Groups[3].Value;

            this.Content = match.Groups[4].Value;
        }

        public enum FacilityType
        {
            Kern, User, Mail, Daemon, Auth, Syslog, LPR, News, UUCP, Cron, AuthPriv, FTP, NTP,
            Audit, Audit2, CRON2, Local0, Local1, Local2, Local3, Local4, Local5, Local6, Local7
        };

        public enum SeverityType { Emergency, Alert, Critical, Error, Warning, Notice, Informational, Debug };
    }
}
