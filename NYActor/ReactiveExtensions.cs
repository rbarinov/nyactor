using System.Reactive.Linq;

namespace NYActor;

public static class ReactiveExtensions
{
    public static IObservable<TSource> RepeatAfterDelay<TSource>(
        this IObservable<TSource> source,
        TimeSpan delay
    )
    {
        return source
            .Concat(
                Observable.Create<TSource>(
                    async observer =>
                    {
                        await Task.Delay(delay);
                        observer.OnCompleted();
                    }
                )
            )
            .Repeat();
    }
}
