using NYActor.Core.Extensions;

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

        public IExpressionCallable<TActor> GetActor<TActor>(string key) where TActor : Actor
        {
            IActorWrapper<TActor> actor = Node.GetActor<TActor>(key).Unwrap();

            return new ExpressionCallable<TActor>(new RequestPropagationActorWrapper<TActor>(actor, RequestPropagationExecutionContext));
        }

        public IExpressionCallable<TActor> GetActor<TActor>() where TActor : Actor
        {
            return new ExpressionCallable<TActor>(new RequestPropagationActorWrapper<TActor>(
                Node.GetActor<TActor>().Unwrap(),
                RequestPropagationExecutionContext
            ));
        }

    }
}
