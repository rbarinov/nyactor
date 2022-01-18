namespace NYActor.Core.RequestPropagation
{
    public class RequestPropagationNodeWrapper : IActorSystem
    {
        public IActorSystem Node { get; }
        public RequestPropagationExecutionContext RequestPropagationExecutionContext { get; }

        public RequestPropagationNodeWrapper(
            IActorSystem node,
            RequestPropagationExecutionContext requestPropagationExecutionContext
        )
        {
            Node = node;
            RequestPropagationExecutionContext = requestPropagationExecutionContext;
        }

        public IActorWrapper<TActor> GetActor<TActor>(string key) where TActor : Actor
        {
            IActorWrapper<TActor> actor = Node.GetActor<TActor>(key);

            return new RequestPropagationActorWrapper<TActor>(actor, RequestPropagationExecutionContext);
        }

        public IActorWrapper<TActor> GetActor<TActor>() where TActor : Actor
        {
            return new RequestPropagationActorWrapper<TActor>(
                Node.GetActor<TActor>(),
                RequestPropagationExecutionContext
            );
        }
    }
}
