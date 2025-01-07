using System.Collections.Generic;
using System.Text;

#region Rfc info
/*
3.3.14. TXT RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   TXT-DATA                    /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

TXT-DATA        One or more <character-string>s.

TXT RRs are used to hold descriptive text.  The semantics of the text
depends on the domain where it is found.
 * 
*/
#endregion

namespace Heijden.DNS
{
    class RecordTXT : Record
    {
        public List<string> TXT;

        public RecordTXT(RecordReader rr, int Length)
        {
            var pos = rr.Position;
            TXT = new List<string>();
            while (
                ((rr.Position - pos) < Length) &&
                (rr.Position < rr.Length)
                )
            {
                TXT.Add(rr.ReadString());
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var txt in TXT)
                sb.AppendFormat("\"{0}\" ", txt);
            return sb.ToString().TrimEnd();
        }

    }
}
