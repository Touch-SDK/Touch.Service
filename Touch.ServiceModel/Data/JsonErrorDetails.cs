using System.Runtime.Serialization;

namespace Touch.ServiceModel
{
    [DataContract]
    public sealed class JsonErrorDetails
    {
        [DataMember(Name = "code")]
        public short Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "reason")]
        public string Reason { get; set; }

        [DataMember(Name = "type")]
        public string ExceptionType { get; set; }

        [DataMember(Name = "stack")]
        public string ErrorStack { get; set; }
    }
}
