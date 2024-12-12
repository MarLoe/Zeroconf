using System;
using System.Collections.Generic;
using System.Linq;

namespace Heijden.DNS
{
    class Response
    {
        /// <summary>
        /// List of Question records
        /// </summary>
        public readonly List<Question> Questions = new();

        /// <summary>
        /// List of AnswerRR records
        /// </summary>
        public readonly List<AnswerRR> Answers = new();

        /// <summary>
        /// List of AuthorityRR records
        /// </summary>
        public readonly List<AuthorityRR> Authorities = new();

        /// <summary>
        /// List of AdditionalRR records
        /// </summary>
        public readonly List<AdditionalRR> Additionals = new();

        public readonly Header header;

        /// <summary>
        /// Error message, empty when no error
        /// </summary>
        public readonly string Error;

        /// <summary>
        /// The Size of the message
        /// </summary>
        public readonly int MessageSize;

        /// <summary>
        /// TimeStamp when cached
        /// </summary>
        public readonly DateTime TimeStamp;

        ///// <summary>
        ///// Server which delivered this response
        ///// </summary>
        //public IPEndPoint Server;

        public Response()
        {
            //	Server = new IPEndPoint(0,0);
            Error = "";
            MessageSize = 0;
            TimeStamp = DateTime.Now;
            header = new Header();
        }

        public Response(/*IPEndPoint iPEndPoint,*/ byte[] data)
        {
            Error = "";
            //Server = iPEndPoint;
            TimeStamp = DateTime.Now;
            MessageSize = data.Length;
            var rr = new RecordReader(data);

            header = new Header(rr);

            if (header.RCODE is not RCode.NoError)
            {
                Error = header.RCODE.ToString();
            }

            for (var intI = 0; intI < header.QDCOUNT; intI++)
            {
                Questions.Add(new Question(rr));
            }

            for (var intI = 0; intI < header.ANCOUNT; intI++)
            {
                Answers.Add(new AnswerRR(rr));
            }

            for (var intI = 0; intI < header.NSCOUNT; intI++)
            {
                Authorities.Add(new AuthorityRR(rr));
            }

            for (var intI = 0; intI < header.ARCOUNT; intI++)
            {
                Additionals.Add(new AdditionalRR(rr));
            }
        }

        ///// <summary>
        ///// List of RecordMX in Response.Answers
        ///// </summary>
        //public RecordMX[] RecordsMX => GetAnswersOfType<RecordMX>().ToArray();

        /// <summary>
        /// List of RecordSRV in Response.Answers
        /// </summary>
        public RecordSRV[] RecordSRV => GetAnswersOfType<RecordSRV>().ToArray();

        /// <summary>
        /// List of RecordTXT in Response.Answers
        /// </summary>
        public RecordTXT[] RecordsTXT => GetAnswersOfType<RecordTXT>().ToArray();

        /// <summary>
        /// List of RecordA in Response.Answers
        /// </summary>
        public RecordA[] RecordsA => GetAnswersOfType<RecordA>().ToArray();

        /// <summary>
        /// List of RecordPTR in Response.Answers
        /// </summary>
        public RecordPTR[] RecordsPTR => GetAnswersOfType<RecordPTR>().ToArray();

        ///// <summary>
        ///// List of RecordCNAME in Response.Answers
        ///// </summary>
        //public RecordCNAME[] RecordsCNAME => GetAnswersOfType<RecordCNAME>().ToArray();

        /// <summary>
        /// List of RecordAAAA in Response.Answers
        /// </summary>
        public RecordAAAA[] RecordsAAAA => GetAnswersOfType<RecordAAAA>().ToArray();

        ///// <summary>
        ///// List of RecordNS in Response.Answers
        ///// </summary>
        //public RecordNS[] RecordsNS => GetAnswersOfType<RecordNS>().ToArray();

        ///// <summary>
        ///// List of RecordSOA in Response.Answers
        ///// </summary>
        //public RecordSOA[] RecordsSOA => GetAnswersOfType<RecordSOA>().ToArray();

        public RR[] RecordsRR =>
            Answers.OfType<RR>().Concat(Authorities).Concat(Additionals).ToArray();

        public bool IsQueryResponse => header.QR;

        private IEnumerable<TRecordType> GetAnswersOfType<TRecordType>() where TRecordType : Record
        {
            return Answers.Select(a => a.RECORD).OfType<TRecordType>();
        }
    }
}
