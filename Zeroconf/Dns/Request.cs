using System.Collections.Generic;
using System.Linq;

namespace Heijden.DNS
{
    class Request
    {
        public Header header;
        readonly List<Question> questions;

        public Request() : this(Enumerable.Empty<Question>())
        {
        }

        public Request(IEnumerable<Question> questions)
        {
            header = new Header
            {
                OPCODE = OPCode.Query,
                QDCOUNT = 0
            };

            this.questions = questions.ToList();
        }

        public void AddQuestion(Question question)
        {
            questions.Add(question);
        }

        public byte[] Data
        {
            get
            {
                var data = new List<byte>();
                header.QDCOUNT = (ushort)questions.Count;
                data.AddRange(header.Data);
                foreach (var q in questions)
                    data.AddRange(q.Data);
                return data.ToArray();
            }
        }
    }
}
