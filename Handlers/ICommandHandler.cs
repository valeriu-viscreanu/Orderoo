namespace OrderApi.Handlers
{
    public interface ICommandHandler<in TCommand, TResult>
    {
        Task<TResult> Handle(
            TCommand command,
            CancellationToken cancellationToken = default);
    }
}
