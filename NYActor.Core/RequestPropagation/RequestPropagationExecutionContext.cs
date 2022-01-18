using System.Collections.Generic;

namespace NYActor.Core.RequestPropagation
{
    public class RequestPropagationExecutionContext : ActorExecutionContext
    {
        public Dictionary<string, string> RequestPropagationValues { get; }

        public RequestPropagationExecutionContext(Dictionary<string, string> requestPropagationValues)
        {
            RequestPropagationValues = requestPropagationValues;
        }
    }
}