using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using Touch.Messaging;

namespace Touch.ServiceModel.Dispatcher
{
    public sealed class JsonErrorHandler : IErrorHandler
    {
        public bool ShowExceptionDetails { get; set; }

        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            var jsonError = new JsonErrorDetails
            {
                Message = error.Message,
                ExceptionType = error.GetType().Name,
                ErrorStack = error.StackTrace,
                Code = (short)HttpStatusCode.InternalServerError,
                Name = "InternalServerError"
            };

            var rmp = new HttpResponseMessageProperty
            {
                StatusCode = HttpStatusCode.InternalServerError,
                StatusDescription = "Internal server error",
            };

            if (error is WebFaultException)
            {
                var err = (WebFaultException) error;
                jsonError.Code = (short)err.StatusCode;
                jsonError.Message = err.Message;
                jsonError.Name = err.StatusCode.ToString("G");
                rmp.StatusCode = err.StatusCode;
                rmp.StatusDescription = err.Message;
            }
            else if (error is FaultException<FaultMessage>)
            {
                var err = (FaultException<FaultMessage>)error;
                jsonError.Code = (short) HttpStatusCode.BadRequest;
                jsonError.Message = err.Detail.Message;
                jsonError.Name = "BadRequest";
                jsonError.Reason = err.Reason.ToString();
                rmp.StatusCode = HttpStatusCode.BadRequest;
                rmp.StatusDescription = err.Message;
            }

            if (!ShowExceptionDetails)
            {
                jsonError.ErrorStack = null;
                jsonError.ExceptionType = null;

                if (jsonError.Code == (short) HttpStatusCode.InternalServerError)
                {
                    jsonError.Message = "Internal server error";
                }
            }

            fault = Message.CreateMessage(version, string.Empty, jsonError, new DataContractJsonSerializer(typeof(JsonErrorDetails)));

            var wbf = new WebBodyFormatMessageProperty(WebContentFormat.Json);
            fault.Properties.Add(WebBodyFormatMessageProperty.Name, wbf);

            rmp.Headers[HttpResponseHeader.ContentType] = "application/json";
            fault.Properties.Add(HttpResponseMessageProperty.Name, rmp);
        }
    }
}
